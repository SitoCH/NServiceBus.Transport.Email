using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using NServiceBus.Logging;

namespace NServiceBus.Transport.Email.Utils
{
    public class ImapUtils
    {
        private static readonly ILog _log = LogManager.GetLogger<ImapUtils>();

        private static string GetErrorMailboxName(string endpointName)
        {
            return $"NSB.{endpointName}.error";
        }

        public static IMailFolder GetErrorMailbox(ImapClient client, string endpointName)
        {
            var toplevel = client.GetFolder(client.PersonalNamespaces[0].Path);
            return toplevel.GetSubfolder(GetErrorMailboxName(endpointName));
        }

        private static string GetPendingMailboxName(string endpointName)
        {
            return $"NSB.{endpointName}.pending";
        }

        public static IMailFolder GetPendingMailbox(ImapClient client, string endpointName)
        {
            var toplevel = client.GetFolder(client.PersonalNamespaces[0].Path);
            return toplevel.GetSubfolder(GetPendingMailboxName(endpointName));
        }

        public static void InitMailboxes(ImapClient client, string endpointName)
        {
            var availableMailboxes = client.GetFolders(client.PersonalNamespaces[0]);
            var toplevel = client.GetFolder(client.PersonalNamespaces[0].Path);

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
        }


        public static void PurgeMailboxes(ImapClient client, string endpointName)
        {
            var pendingMailbox = GetPendingMailbox(client, endpointName);
            if (!pendingMailbox.Exists)
                return;

            pendingMailbox.Open(FolderAccess.ReadWrite);
            var uids = pendingMailbox.Search(SearchQuery.All);
            if (uids.Any())
            {
                DeleteMessages(client, pendingMailbox, uids);
            }

            pendingMailbox.Close();
        }

        public static void DeleteMessages(ImapClient client, IMailFolder mailbox, IList<UniqueId> uids)
        {
            if (client.Capabilities.HasFlag(ImapCapabilities.UidPlus))
            {
                mailbox.Expunge(uids);
            }
            else
            {
                mailbox.AddFlags(uids, MessageFlags.Deleted, true);
                mailbox.Expunge();
            }
        }
    }
}