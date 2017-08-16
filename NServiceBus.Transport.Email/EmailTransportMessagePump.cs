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
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email
{
    internal class EmailTransportMessagePump : IPushMessages
    {
        private static readonly ILog _log = LogManager.GetLogger<EmailTransportMessagePump>();
        private static bool _started;

        private readonly string _endpointName;

        private CancellationToken _cancellationToken;
        private CancellationTokenSource _cancellationTokenSource;
        private SemaphoreSlim _concurrencyLimiter;
        private Task _messagePumpTask;
        private Func<ErrorContext, Task<ErrorHandleResult>> _onError;
        private Func<MessageContext, Task> _pipeline;
        private bool _purgeOnStartup;
        private ConcurrentDictionary<Task, Task> _runningReceiveTasks;

        public EmailTransportMessagePump(string endpointName)
        {
            _endpointName = endpointName;
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

            _runningReceiveTasks = new ConcurrentDictionary<Task, Task>();
            _concurrencyLimiter = new SemaphoreSlim(limitations.MaxConcurrency);
            _cancellationTokenSource = new CancellationTokenSource();

            _cancellationToken = _cancellationTokenSource.Token;

            if (_purgeOnStartup)
            {
                ImapUtils.PurgeMailboxes(_endpointName);
            }

            _messagePumpTask = Task.Factory.StartNew(ProcessMessages, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        public async Task Stop()
        {
            _cancellationTokenSource.Cancel();

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);
            var allTasks = _runningReceiveTasks.Values.Concat(new[]
            {
                _messagePumpTask
            });
            var finishedTask = await Task.WhenAny(Task.WhenAll(allTasks), timeoutTask).ConfigureAwait(false);

            if (finishedTask.Equals(timeoutTask))
            {
                _log.Error("The message pump failed to stop with in the time allowed(30s).");
            }

            _concurrencyLimiter.Dispose();
            _runningReceiveTasks.Clear();
        }

        [DebuggerNonUserCode]
        private async Task ProcessMessages()
        {
            try
            {
                await InnerProcessMessages().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // For graceful shutdown purposes
            }
            catch (Exception ex)
            {
                _log.Error("Email message pump failed.", ex);
            }

            if (!_cancellationToken.IsCancellationRequested)
            {
                await ProcessMessages().ConfigureAwait(false);
            }

        }

        private async Task InnerProcessMessages()
        {
            using (var client = ImapUtils.GetImapClient())
            {
                if (!client.Capabilities.HasFlag(ImapCapabilities.Idle))
                {
                    throw new Exception("Server does not support IMAP IDLE");
                }

                var query = SearchQuery.SubjectContains($"NSB-MSG-{_endpointName}-");

                // Listen to new messages
                client.Inbox.MessagesArrived += async (o, args) =>
                {
                    _log.Info("IDLE notification from IMAP server.");
                    foreach (var m in client.Inbox.Search(query))
                    {
                        _log.Info($"Received message with UID: {m}.");
                        await ProcessMessage(client, m);
                    }
                };
                // Process any messages that arrived when the endpoint was unactive
                foreach (var m in client.Inbox.Search(query))
                {
                    _log.Info($"Found pre-existing message with UID: {m}.");
                    await ProcessMessage(client, m);
                }

                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    await Task.Delay(750, _cancellationToken).ConfigureAwait(false);
                }

                // ReSharper disable once MethodSupportsCancellation
                await client.DisconnectAsync(true);
            }
        }

        private async Task ProcessMessage(ImapClient client, UniqueId messageId)
        {
            await _concurrencyLimiter.WaitAsync(_cancellationToken).ConfigureAwait(false);

            var task = Task.Run(async () =>
            {
                try
                {
                    await ProcessMessageWithTransaction(client, messageId).ConfigureAwait(false);
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }, _cancellationToken);

            await task.ContinueWith(t =>
                {
                    Task toBeRemoved;
                    _runningReceiveTasks.TryRemove(t, out toBeRemoved);
                },
                TaskContinuationOptions.ExecuteSynchronously); //.Ignore();

            await _runningReceiveTasks.AddOrUpdate(task, task, (k, v) => task); //.Ignore();
        }

        private async Task ProcessMessageWithTransaction(ImapClient client, UniqueId messageId)
        {
            using (var transaction = new MailBasedTransaction(client, _endpointName))
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

                var ms = new MemoryStream();
                message.Attachments.First().WriteTo(ms);
                var body = ms.ToArray();
                var transportTransaction = new TransportTransaction();
                transportTransaction.Set(transaction);

                var shouldCommit = await HandleMessageWithRetries(messageNativeId, headers, body, transportTransaction, 1).ConfigureAwait(false);

                if (shouldCommit)
                {
                    transaction.Commit();
                }
            }
        }

        private async Task<bool> HandleMessageWithRetries(string messageId, Dictionary<string, string> headers, byte[] body, TransportTransaction transportTransaction, int processingAttempt)
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

                await _pipeline(pushContext).ConfigureAwait(false);

                return !receiveCancellationTokenSource.IsCancellationRequested;
            }
            catch (Exception e)
            {
                var errorContext = new ErrorContext(e, headers, messageId, body, transportTransaction, processingAttempt);
                var errorHandlingResult = await _onError(errorContext).ConfigureAwait(false);

                if (errorHandlingResult == ErrorHandleResult.RetryRequired)
                {
                    return await HandleMessageWithRetries(messageId, headers, body, transportTransaction, ++processingAttempt).ConfigureAwait(false);
                }

                return true;
            }
        }
    }
}