using System.Configuration;
using System.Data.Common;
using System.Linq;
using NServiceBus.Logging;
using S22.Imap;

namespace NServiceBus.Transport.Email.Utils
{
    public class ImapUtils
    {
        private static readonly ILog _log = LogManager.GetLogger<ImapUtils>();


        public static IImapClient GetImapClient()
        {
            var imapCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/IMAP"];
            var imapBuilder = new DbConnectionStringBuilder { ConnectionString = imapCS.ConnectionString };
            return new ImapClient(imapBuilder["server"].ToString(), int.Parse(imapBuilder["port"].ToString()), imapBuilder["user"].ToString(), imapBuilder["password"].ToString(), AuthMethod.Login, true);
        }

        public static string GetEmailUser()
        {
            var imapCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/IMAP"];
            var imapBuilder = new DbConnectionStringBuilder { ConnectionString = imapCS.ConnectionString };
            return imapBuilder["user"].ToString();
        }

        public static string GetCommittedMailboxName(string endpointName)
        {
            return string.Format("NSB.{0}.committed", endpointName);
        }

        public static string GetPendingMailboxName(string endpointName)
        {
            return string.Format("NSB.{0}.pending", endpointName);
        }

        public static void InitMailboxes(string endpointName)
        {
            using (var imapClient = GetImapClient())
            {
                var availableMailboxes = imapClient.ListMailboxes().ToList();

                var committedMailboxName = GetCommittedMailboxName(endpointName);
                if (!availableMailboxes.Contains(committedMailboxName))
                {
                    imapClient.CreateMailbox(committedMailboxName);
                    _log.Info(string.Format("Created new committed mailbox: {0}", committedMailboxName));
                }
                var pendingMailboxName = GetPendingMailboxName(endpointName);
                if (!availableMailboxes.Contains(pendingMailboxName))
                {
                    imapClient.CreateMailbox(pendingMailboxName);
                    _log.Info(string.Format("Created new pending mailbox: {0}", pendingMailboxName));
                }
            }
        }

        public static void PurgeMailboxes(string endpointName)
        {
            using (var imapClient = GetImapClient())
            {
                PurgeMailbox(imapClient, GetCommittedMailboxName(endpointName));
            }
        }

        private static void PurgeMailbox(IImapClient imapClient, string mailbox)
        {
            foreach (var message in imapClient.Search(SearchCondition.All(), mailbox))
            {
                imapClient.DeleteMessage(message, mailbox);
            }
        }
    }
}
