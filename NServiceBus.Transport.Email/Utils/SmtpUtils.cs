using System.Configuration;
using System.Data.Common;
using System.IO;
using System.Net;
using System.Net.Mail;
using FluentEmail.Smtp;
using Attachment = FluentEmail.Core.Models.Attachment;

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

            var email = FluentEmail.Core.Email
                .From(imapBuilder["user"].ToString())
                .To(to)
                .Subject(subject)
                .Body(body)
                .Attach(new Attachment
                {
                    Data = new MemoryStream(attachment),
                    Filename = "body",
                    ContentType = "application/octet-stream"
                });

            email.Sender = new SmtpSender(new SmtpClient
            {
                Host = smtpBuilder["server"].ToString(),
                Port = int.Parse(smtpBuilder["port"].ToString()),
                Credentials = new NetworkCredential(smtpBuilder["user"].ToString(), smtpBuilder["password"].ToString())
            });
            email.Send();
        }
    }
}
