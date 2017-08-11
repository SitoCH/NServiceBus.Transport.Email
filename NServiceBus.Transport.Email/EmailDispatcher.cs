using System.IO;
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
                    .From(_settings.Get<string>("mail.sender"))
                    .To(operation.Destination)
                    .Subject(operation.Message.MessageId)
                    .Body(serializedHeaders)
                    .Attach(new Attachment
                    {
                        Data = new MemoryStream(operation.Message.Body),
                        Filename = "body",
                        ContentType = "application/octet-stream"
                    });

                email.Sender = new SmtpSender(new SmtpClient { Host = _settings.Get<string>("mail.host"), Port = _settings.Get<int>("mail.port") });
                email.Send();
            }

            return Task.CompletedTask;
        }
    }
}
