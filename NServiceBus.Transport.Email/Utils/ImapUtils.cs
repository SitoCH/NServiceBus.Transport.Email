using System.Configuration;
using System.Data.Common;
using System.Linq;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using NServiceBus.Logging;

namespace NServiceBus.Transport.Email.Utils
{
    public class ImapUtils
    {
        private static readonly ILog _log = LogManager.GetLogger<ImapUtils>();


        public static ImapClient GetImapClient()
        {
            var imapCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/IMAP"];
            var imapBuilder = new DbConnectionStringBuilder { ConnectionString = imapCS.ConnectionString };

            // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
            var client = new ImapClient { ServerCertificateValidationCallback = (s, c, h, e) => true };

            client.Connect(imapBuilder["server"].ToString(), int.Parse(imapBuilder["port"].ToString()), false);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(imapBuilder["user"].ToString(), imapBuilder["password"].ToString());

            return client;
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
            using (var client = GetImapClient())
            {
                var availableMailboxes = client.GetFolders(client.PersonalNamespaces[0]);
                var toplevel = client.GetFolder(client.PersonalNamespaces[0]);

                var errorMailboxName = GetErrorMailboxName(endpointName);
                if (availableMailboxes.All(x => x.Name != errorMailboxName))
                {
                    toplevel.Create(errorMailboxName, true);
                    _log.Info($"Created new error mailbox: {errorMailboxName}");
                }
                var pendingMailboxName = GetPendingMailboxName(endpointName);
                if (availableMailboxes.All(x => x.Name != pendingMailboxName))
                {
                    toplevel.Create(pendingMailboxName, true);
                    _log.Info($"Created new pending mailbox: {pendingMailboxName}");
                }

                client.Disconnect(true);
            }
        }

        public static void PurgeMailboxes(string endpointName)
        {
            using (var client = GetImapClient())
            {
                var toplevel = client.GetFolder(client.PersonalNamespaces[0]);
                var pendingMailbox = toplevel.GetSubfolder(GetPendingMailboxName(endpointName));
                if (pendingMailbox.Exists)
                {
                    var uids = pendingMailbox.Search(SearchQuery.All);
                    pendingMailbox.AddFlags(uids, MessageFlags.Deleted, true);
                    pendingMailbox.Expunge();
                }
                client.Disconnect(true);
            }
        }
    }
}
