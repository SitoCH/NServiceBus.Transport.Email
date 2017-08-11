using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using FluentEmail.Smtp;
using NServiceBus.Extensibility;
using NServiceBus.Settings;
using NServiceBus.Transport.Email.Utils;
using Attachment = FluentEmail.Core.Models.Attachment;

namespace NServiceBus.Transport.Email
{
    internal class EmailDispatcher : IDispatchMessages
    {
        private readonly SettingsHolder _settings;

        public EmailDispatcher(SettingsHolder settings)
        {
            _settings = settings;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            foreach (var operation in outgoingMessages.UnicastTransportOperations)
            {
                var serializedHeaders = HeaderSerializer.Serialize(operation.Message.Headers);

                var email = FluentEmail.Core.Email
                    .From(_settings.Get<string>(Constants.ENDPOINT_NAME))
                    .To(operation.Destination)
                    .Subject(operation.Message.MessageId)
                    .Body(serializedHeaders)
                    .Attach(new Attachment
                    {
                        Data = new MemoryStream(operation.Message.Body),
                        Filename = "body",
                        ContentType = "application/octet-stream"
                    });

                email.Sender = new SmtpSender(new SmtpClient
                {
                    Host = _settings.Get<string>(Constants.SMTP_HOST),
                    Port = _settings.Get<int>(Constants.SMTP_HOST_PORT),
                    Credentials = new NetworkCredential(_settings.Get<string>(Constants.SMTP_USER), _settings.Get<string>(Constants.SMTP_PASSWORD))
                });
                email.Send();
            }

            return Task.CompletedTask;
        }
    }
}
