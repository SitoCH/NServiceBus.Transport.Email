using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static bool _started;

        private readonly string _endpointName;
        private readonly SettingsHolder _settings;
        private CancellationTokenSource _timeoutTokenSource;
        private CancellationTokenSource _cancellationTokenSource;
        private Func<ErrorContext, Task<ErrorHandleResult>> _onError;
        private Func<MessageContext, Task> _pipeline;

        private bool _purgeOnStartup;

        public EmailTransportMessagePump(SettingsHolder settings)
        {
            _endpointName = settings.EndpointName();
            _settings = settings;
        }

        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            _onError = onError;
            _pipeline = onMessage;
            _purgeOnStartup = settings.PurgeOnStartup;
            return Task.CompletedTask;
        }

        public void Start(PushRuntimeSettings limitations)
        {
            if (_started)
                return;
            _started = true;

            Task.Factory.StartNew(() =>
            {
                using (var client = _settings.GetImapClient())
                {
                    ImapUtils.InitMailboxes(client, _settings.EndpointName());

                    client.Inbox.Open(FolderAccess.ReadWrite);


                    if (_purgeOnStartup)
                    {
                        ImapUtils.PurgeMailboxes(client, _endpointName);
                    }

                    var query = SearchQuery.SubjectContains($"NSB-MSG-{_endpointName}-");
                    // Process any messages that arrived when the endpoint was unactive
                    foreach (var m in client.Inbox.Search(query))
                    {
                        _log.Info($"Found pre-existing message with UID: {m}.");
                        ProcessMessageWithTransaction(client, m);
                    }

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
                                if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                                {
                                    _log.Info("Waiting for IDLE from IMAP server.");
                                    client.Idle(_timeoutTokenSource.Token, _cancellationTokenSource.Token);
                                }
                                else
                                {
                                    _log.Info("Waiting for new messages...");
                                    client.NoOp(_cancellationTokenSource.Token);
                                    WaitHandle.WaitAny(new[] {_timeoutTokenSource.Token.WaitHandle, _cancellationTokenSource.Token.WaitHandle});
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                break;
                            }
                            catch (ImapProtocolException)
                            {
                                break;
                            }
                            catch (ImapCommandException)
                            {
                                break;
                            }

                            foreach (var m in client.Inbox.Search(query))
                            {
                                _log.Info($"Processing message with UID: {m}.");
                                ProcessMessageWithTransaction(client, m);
                            }
                        }
                    }
                }
            });
        }

        public Task Stop()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            catch (OperationCanceledException)
            {
            }

            return Task.CompletedTask;
        }

        private void ProcessMessageWithTransaction(ImapClient client, UniqueId messageId)
        {
            var transaction = new MailBasedTransaction(client, _endpointName);
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

            var ms = new MemoryStream();
            ((MimePart) message.Attachments.First()).ContentObject.DecodeTo(ms);
            var body = ms.ToArray();
            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(transaction);

            var shouldCommit = HandleMessageWithRetries(messageNativeId, headers, body, transportTransaction, 1);

            if (shouldCommit)
            {
                transaction.Commit();
            }

            transaction.Finalize();
        }

        private bool HandleMessageWithRetries(string messageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction, int processingAttempt)
        {
            try
            {
                var receiveCancellationTokenSource = new CancellationTokenSource();
                var pushContext = new MessageContext(
                    messageId,
                    new Dictionary<string, string>(headers),
                    body,
                    transportTransaction,
                    receiveCancellationTokenSource,
                    new ContextBag());

                _pipeline(pushContext);

                return !receiveCancellationTokenSource.IsCancellationRequested;
            }
            catch (Exception e)
            {
                var errorContext = new ErrorContext(e, headers, messageId, body, transportTransaction, processingAttempt);
                var errorHandlingResult = _onError(errorContext);
                errorHandlingResult.Wait();
                if (errorHandlingResult.Result == ErrorHandleResult.RetryRequired)
                {
                    return HandleMessageWithRetries(messageId, headers, body, transportTransaction, ++processingAttempt);
                }

                return true;
            }
        }
    }
}