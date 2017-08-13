using System;
using S22.Imap;

namespace NServiceBus.Transport.Email.Utils
{
    internal class MailBasedTransaction : IDisposable
    {
        private readonly IImapClient _client;
        private readonly string _endpointName;
        private bool _committed;
        private uint _messageId;

        public MailBasedTransaction(IImapClient client, string endpointName)
        {
            _client = client;
            _endpointName = endpointName;
        }

        public void BeginTransaction(uint messageId)
        {
            _messageId = messageId;
            _client.MoveMessage(messageId, ImapUtils.GetPendingMailboxName(_endpointName), _client.DefaultMailbox);
        }

        public void Commit() => _committed = true;

        public void Dispose()
        {
            if (!_committed)
            {
                // rollback by moving the message back to the DefaultMailbox
                _client.MoveMessage(_messageId, _client.DefaultMailbox, ImapUtils.GetPendingMailboxName(_endpointName));
            }
            else
            {
                _client.MoveMessage(_messageId, ImapUtils.GetCommittedMailboxName(_endpointName), ImapUtils.GetPendingMailboxName(_endpointName));
            }
        }
    }
}
