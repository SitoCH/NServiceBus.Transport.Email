using System;
using System.Collections.Generic;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using NServiceBus.Logging;

namespace NServiceBus.Transport.Email.Utils
{
    internal class MailBasedTransaction : IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger<EmailTransportMessagePump>();

        private readonly ImapClient _client;
        private readonly string _endpointName;
        private bool _aborted;
        private bool _committed;
        private string _messageId;

        public MailBasedTransaction(ImapClient client, string endpointName)
        {
            _client = client;
            _endpointName = endpointName;
        }

        public Tuple<string, MimeMessage> BeginTransaction(UniqueId messageId)
        {
            var message = _client.Inbox.GetMessage(messageId);
            _messageId = message.Subject.Replace($"NSB-MSG-{_endpointName}-", string.Empty);
            _client.Inbox.MoveTo(messageId, ImapUtils.GetPendingMailbox(_client, _endpointName));
            return new Tuple<string, MimeMessage>(_messageId, message);
        }

        public void Commit() => _committed = true;

        private UniqueId GetMessageUIDFromId()
        {
            var query = SearchQuery.SubjectContains($"NSB-MSG-{_endpointName}-{_messageId}");
            foreach (var messageId in ImapUtils.GetPendingMailbox(_client, _endpointName).Search(query))
            {
                return messageId;
            }

            throw new Exception($"Pending message not found for id {_messageId}.");
        }

        public void Dispose()
        {
            var pendingFolder = ImapUtils.GetPendingMailbox(_client, _endpointName);
            pendingFolder.Open(FolderAccess.ReadWrite);
            try
            {
                var messageId = new List<UniqueId> {GetMessageUIDFromId()};
                if (_aborted)
                {
                    _log.Debug($"Move message {_messageId} in the error mailbox due to failed commit.");
                    pendingFolder.MoveTo(messageId, ImapUtils.GetErrorMailbox(_client, _endpointName));
                }
                else if (!_committed)
                {
                    _log.Debug($"Rollback message {_messageId} due to timeout on commit.");
                    pendingFolder.MoveTo(messageId, _client.Inbox);
                }
                else
                {
                    _log.Debug($"Commit successful, delete message {_messageId}.");
                    ImapUtils.DeleteMessages(_client, pendingFolder, messageId);
                }
            }
            finally
            {
                pendingFolder.Close();
            }

            _client.Inbox.Open(FolderAccess.ReadWrite);
        }

        public void Abort()
        {
            _aborted = true;
        }
    }
}