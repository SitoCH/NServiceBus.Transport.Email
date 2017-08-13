using System.Configuration;
using System.Data.Common;
using System.Linq;
using S22.Imap;

namespace NServiceBus.Transport.Email.Utils
{
    public static class ImapUtils
    {
        public static ImapClient GetImapClient()
        {
            var imapCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/IMAP"];
            var imapBuilder = new DbConnectionStringBuilder { ConnectionString = imapCS.ConnectionString };
            return new ImapClient(imapBuilder["server"].ToString(), int.Parse(imapBuilder["port"].ToString()), imapBuilder["user"].ToString(), imapBuilder["password"].ToString(), AuthMethod.Login, true);
        }

        public static void InitMailbox()
        {
            using (var imapClient = GetImapClient())
            {
                var availableMailboxes = imapClient.ListMailboxes().ToList();
                if (!availableMailboxes.Contains("NSB.committed"))
                    imapClient.CreateMailbox("NSB.committed");
                if (!availableMailboxes.Contains("NSB.bodies"))
                    imapClient.CreateMailbox("NSB.bodies");
            }
        }
    }
}
