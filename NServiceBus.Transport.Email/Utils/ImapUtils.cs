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

        public static string GetErrorMailboxName(string endpointName)
        {
            return $"NSB.{endpointName}.error";
        }

        public static string GetPendingMailboxName(string endpointName)
        {
            return $"NSB.{endpointName}.pending";
        }

        public static void InitMailboxes(string endpointName)
        {
            using (var imapClient = GetImapClient())
            {
                var availableMailboxes = imapClient.ListMailboxes().ToList();

                var errorMailboxName = GetErrorMailboxName(endpointName);
                if (!availableMailboxes.Contains(errorMailboxName))
                {
                    imapClient.CreateMailbox(errorMailboxName);
                    _log.Info($"Created new error mailbox: {errorMailboxName}");
                }
                var pendingMailboxName = GetPendingMailboxName(endpointName);
                if (!availableMailboxes.Contains(pendingMailboxName))
                {
                    imapClient.CreateMailbox(pendingMailboxName);
                    _log.Info($"Created new pending mailbox: {pendingMailboxName}");
                }
            }
        }

        public static void PurgeMailboxes(string endpointName)
        {
            /*using (var imapClient = GetImapClient())
            {
                PurgeMailbox(imapClient, GetCommittedMailboxName(endpointName));
            }*/
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
