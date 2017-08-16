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
        private bool _committed;
        private string _messageId;

        public MailBasedTransaction(ImapClient client, string endpointName)
        {
            _client = client;
            _endpointName = endpointName;
        }

        private IMailFolder GetPendingFolder()
        {
            return _client.GetFolder(_client.PersonalNamespaces[0]).GetSubfolder(ImapUtils.GetPendingMailboxName(_endpointName));
        }

        public Tuple<string, MimeMessage> BeginTransaction(UniqueId messageId)
        {
            var message = _client.Inbox.GetMessage(messageId);
            _messageId = message.Subject.Replace($"NSB-MSG-{_endpointName}-", string.Empty);
            _client.Inbox.MoveTo(messageId, GetPendingFolder());
            return new Tuple<string, MimeMessage>(_messageId, message);
        }

        public void Commit() => _committed = true;

        private UniqueId GetMessageUIDFromId()
        {
            var query = SearchQuery.SubjectContains($"NSB-MSG-{_endpointName}-{_messageId}");
            foreach (var messageId in GetPendingFolder().Search(query))
            {
                return messageId;
            }
            throw new Exception($"Pending message not found for id {_messageId}.");
        }

        public void Dispose()
        {
            var pendingFolder = GetPendingFolder();
            var messageId = new List<UniqueId> { GetMessageUIDFromId() };

            if (!_committed)
            {
                // rollback by moving the message back to the DefaultMailbox
                _log.Info($"Rollback message {_messageId} due to failed commit.");
                GetPendingFolder().MoveTo(messageId, _client.Inbox);
            }
            else
            {
                _log.Info($"Commit successful, delete message {_messageId}.");
                pendingFolder.AddFlags(messageId, MessageFlags.Deleted, true);
                pendingFolder.Expunge();
            }
        }
    }
}
