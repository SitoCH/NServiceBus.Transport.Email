using System.Configuration;
using System.Data.Common;
using System.IO;
using MailKit.Net.Smtp;
using MimeKit;

namespace NServiceBus.Transport.Email.Utils
{
    public static class SmtpUtils
    {
        public static void SendMail(string to, string subject, string body, byte[] attachment)
        {
            var imapCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/IMAP"];
            var imapBuilder = new DbConnectionStringBuilder { ConnectionString = imapCS.ConnectionString };

            var smtpCS = ConfigurationManager.ConnectionStrings["NServiceBus/Transport/SMTP"];
            var smtpBuilder = new DbConnectionStringBuilder { ConnectionString = smtpCS.ConnectionString };

            var message = new MimeMessage { Subject = subject };
            message.From.Add(new MailboxAddress(imapBuilder["user"].ToString()));
            message.To.Add(new MailboxAddress(to));
            var messageBody = new TextPart("plain") { Text = body };
            var messageAttachment = new MimePart("application", "octet-stream")
            {
                ContentObject = new ContentObject(new MemoryStream(attachment)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = "native-message"
            };

            var multipart = new Multipart("mixed") { messageBody, messageAttachment };
            message.Body = multipart;

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(smtpBuilder["server"].ToString(), int.Parse(smtpBuilder["port"].ToString()), false);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(smtpBuilder["user"].ToString(), smtpBuilder["password"].ToString());

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}
