using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email
{
    internal class EmailTransportMessagePump : IPushMessages
    {
        private static readonly ILog _log = LogManager.GetLogger<EmailTransportMessagePump>();

        private bool _started;
        private readonly string _endpointName;
        private readonly SettingsHolder _settings;
        private CancellationTokenSource _timeoutTokenSource;
        private CancellationTokenSource _cancellationTokenSource;
        private Func<ErrorContext, Task<ErrorHandleResult>> _onError;
        private Func<MessageContext, Task> _onMessage;
        private Task _messagePumpTask;
        private bool _purgeOnStartup;
        private CriticalError _criticalError;
        private readonly ConcurrentDictionary<string, int> _currentRetries = new ConcurrentDictionary<string, int>();

        public EmailTransportMessagePump(SettingsHolder settings)
        {
            _endpointName = settings.EndpointName();
            _settings = settings;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            _onError = onError;
            _onMessage = onMessage;
            _purgeOnStartup = settings.PurgeOnStartup;
            _criticalError = criticalError;
            return Task.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            if (_started)
                return;
            _started = true;

            _messagePumpTask = Task.Factory.StartNew(ProcessMessages,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }

        private async Task ProcessMessages()
        {
            using (var client = _settings.GetImapClient())
            {
                ImapUtils.InitMailboxes(client, _endpointName);

                if (_purgeOnStartup)
                {
                    ImapUtils.PurgeMailboxes(client, _endpointName);
                }

                client.Inbox.Open(FolderAccess.ReadWrite);

                var query = SearchQuery.SubjectContains($"NSB-MSG-{_endpointName}-");

                // Listen to new messages
                void CheckForNewMessages(object o, EventArgs args)
                {
                    _log.Debug("Mailbox count changed.");
                    _timeoutTokenSource.Cancel();
                }

                client.Inbox.CountChanged += CheckForNewMessages;

                _cancellationTokenSource = new CancellationTokenSource();

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    using (_timeoutTokenSource)
                    {
                        try
                        {
                            foreach (var m in client.Inbox.Search(query))
                            {
                                _log.Debug($"Processing message with UID: {m}.");
                                await ProcessMessageWithTransaction(client, m);
                            }

                            if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                            {
                                _log.Debug("Waiting for IDLE from IMAP server.");
                                await client.IdleAsync(_timeoutTokenSource.Token, _cancellationTokenSource.Token);
                            }
                            else
                            {
                                _log.Debug("Waiting for new messages...");
                                client.NoOp(_cancellationTokenSource.Token);
                                WaitHandle.WaitAny(new[] {_timeoutTokenSource.Token.WaitHandle, _cancellationTokenSource.Token.WaitHandle});
                            }
                        }
                        catch (OperationCanceledException)
                        {
                        }
                        catch (ImapProtocolException)
                        {
                            break;
                        }
                        catch (ImapCommandException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message, ex);
                            _criticalError.Raise(ex.Message, ex);
                        }
                    }
                }
            }
        }

        public Task Stop()
        {
            try
            {
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message, ex);
                throw;
            }

            return _messagePumpTask;
        }

        private async Task ProcessMessageWithTransaction(ImapClient client, UniqueId messageId)
        {
            using (var transaction = new MailBasedTransaction(client, _endpointName))
            {
                try
                {
                    var pair = transaction.BeginTransaction(messageId);
                    var messageNativeId = pair.Item1;
                    var message = pair.Item2;

                    var headers = HeaderSerializer.Deserialize(message.TextBody);

                    if (headers.TryGetValue(Headers.TimeToBeReceived, out string ttbrString))
                    {
                        var ttbr = TimeSpan.Parse(ttbrString);
                        var sentTime = message.Date;
                        if (sentTime + ttbr < DateTime.UtcNow)
                        {
                            return;
                        }
                    }

                    var body = new byte[0];
                    if (message.Attachments.Any())
                    {
                        var ms = new MemoryStream();
                        ((MimePart) message.Attachments.First()).Content.DecodeTo(ms);
                        body = ms.ToArray();
                    }

                    var transportTransaction = new TransportTransaction();

                    transportTransaction.Set(transaction);

                    var shouldCommit = await HandleMessageWithRetries(messageNativeId, headers, body, transportTransaction);
                    if (shouldCommit)
                    {
                        transaction.Commit();
                        _currentRetries.Remove(messageNativeId, out _);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error(ex.Message, ex);
                    transaction.Abort();
                    _criticalError.Raise(ex.Message, ex);
                }
            }
        }

        private async Task<bool> HandleMessageWithRetries(string messageId,
            Dictionary<string, string> headers,
            byte[] body,
            TransportTransaction transportTransaction)
        {
            var tokenSource = new CancellationTokenSource();
            var messageContext = new MessageContext(messageId, headers, body, transportTransaction, tokenSource, new ContextBag());

            try
            {
                await _onMessage(messageContext);
            }
            catch (Exception exception)
            {
                const int maxAttempts = 5;
                for (var i = _currentRetries.GetOrAdd(messageId, 0); i < maxAttempts; i++)
                {
                    try
                    {
                        _currentRetries.AddOrUpdate(messageId, i + 1, (k, v) => i + 1);
                        var errorContext = new ErrorContext(exception, headers, messageId, body, transportTransaction, i + 1);
                        var actionToTake = await _onError(errorContext);
                        if (actionToTake == ErrorHandleResult.RetryRequired)
                        {
                            return false;
                        }

                        if (actionToTake == ErrorHandleResult.Handled)
                        {
                            break;
                        }
                    }
                    catch (Exception)
                    {
                        if (i == maxAttempts - 1)
                            throw;
                    }
                }
            }

            if (tokenSource.IsCancellationRequested)
            {
                return false;
            }

            return true;
        }
    }
}