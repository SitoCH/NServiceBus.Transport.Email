using System.Configuration;
using System.Data.Common;
using System.IO;
using MailKit.Net.Smtp;
using MimeKit;

namespace NServiceBus.Transport.Email.Utils
{
    public static class SmtpUtils
    {
        public static void SendMail(string smtpServerUrl, int smtpServerPort, string smtpUser,
            string smtpPassword, string mailFrom, string to, string subject, string body, byte[] attachment)
        {
            var message = new MimeMessage {Subject = subject};
            message.From.Add(new MailboxAddress(mailFrom));
            message.To.Add(new MailboxAddress(to));
            var messageBody = new TextPart("plain") {Text = body};


            if (attachment.Length > 0)
            {
                var messageAttachment = new MimePart("application", "octet-stream")
                {
                    Content = new MimeContent(new MemoryStream(attachment)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = "native-message"
                };

                message.Body = new Multipart("mixed") {messageBody, messageAttachment};
            }
            else
            {
                message.Body = new Multipart("mixed") {messageBody};
            }

            using (var client = new SmtpClient())
            {
                // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                client.Connect(smtpServerUrl, smtpServerPort, false);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(smtpUser, smtpPassword);

                client.Send(message);
                client.Disconnect(true);
            }
        }
    }
}