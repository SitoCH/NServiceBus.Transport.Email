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

        public static string GetPendingMailboxName(string endpointName)
        {
            return $"NSB.{endpointName}.pending";
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
            var toplevel = client.GetFolder(client.PersonalNamespaces[0].Path);
            var pendingMailbox = toplevel.GetSubfolder(GetPendingMailboxName(endpointName));
            if (pendingMailbox.Exists)
            {
                var uids = pendingMailbox.Search(SearchQuery.All);
                pendingMailbox.AddFlags(uids, MessageFlags.Deleted, true);
                pendingMailbox.Expunge();
            }
        }
    }
}