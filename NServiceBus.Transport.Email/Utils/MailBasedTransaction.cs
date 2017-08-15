using System;
using System.Net.Mail;
using NServiceBus.Logging;
using S22.Imap;

namespace NServiceBus.Transport.Email.Utils
{
    internal class MailBasedTransaction : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger<EmailTransportMessagePump>();

        private readonly IImapClient _client;
        private readonly string _endpointName;
        private readonly string _pendingMailboxName;
        private bool _committed;
        private string _messageId;

        public MailBasedTransaction(IImapClient client, string endpointName)
        {
            _client = client;
            _endpointName = endpointName;
            _pendingMailboxName = ImapUtils.GetPendingMailboxName(_endpointName);
        }

        public Tuple<string, MailMessage> BeginTransaction(uint messageUID)
        {
            var message = _client.GetMessage(messageUID, true, _client.DefaultMailbox);
            _messageId = message.Subject.Replace($"NSB-MSG-{_endpointName}-", string.Empty);
            _client.MoveMessage(messageUID, ImapUtils.GetPendingMailboxName(_endpointName), _client.DefaultMailbox);
            return new Tuple<string, MailMessage>(_messageId, message);
        }

        public void Commit() => _committed = true;

        private uint GetMessageUIDFromId()
        {
            foreach (var messageUID in _client.Search(SearchCondition.All(), _pendingMailboxName))
            {

                var messageHeaders = _client.GetMessage(messageUID, FetchOptions.HeadersOnly, false, _pendingMailboxName);
                if (messageHeaders.Subject.Contains(_messageId))
                {
                    return messageUID;
                }
            }
            throw new Exception($"Pending message not found for id {_messageId}.");
        }

        public void Dispose()
        {
            var messageUID = GetMessageUIDFromId();

            if (!_committed)
            {
                // rollback by moving the message back to the DefaultMailbox
                _log.Info($"Rollback message {_messageId} due to failed commit.");
                _client.MoveMessage(messageUID, _client.DefaultMailbox, _pendingMailboxName);
            }
            else
            {
                _log.Info($"Commit successful, delete message {_messageId}.");
                _client.DeleteMessage(messageUID, _pendingMailboxName);
            }
        }
    }
}
