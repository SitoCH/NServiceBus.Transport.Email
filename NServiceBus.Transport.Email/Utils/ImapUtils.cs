using System.Configuration;
using System.Data.Common;
using System.Linq;
using S22.Imap;

namespace NServiceBus.Transport.Email.Utils
{
    public static class ImapUtils
    {
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


                if (!availableMailboxes.Contains(GetCommittedMailboxName(endpointName)))
                    imapClient.CreateMailbox(GetCommittedMailboxName(endpointName));
                if (!availableMailboxes.Contains(GetPendingMailboxName(endpointName)))
                    imapClient.CreateMailbox(GetPendingMailboxName(endpointName));
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
