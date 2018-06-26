using System;
using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Settings;
using NServiceBus.Transport.Email.Utils;

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
                var queueIndex = operation.Destination.IndexOf("@", StringComparison.Ordinal);
                var transportSettings = _settings.getTransportSettings();
                
                SmtpUtils.SendMail(
                    transportSettings.SmtpServer,
                    transportSettings.SmtpServerPort,
                    transportSettings.SmtpUser,
                    transportSettings.SmtpPassword,
                    transportSettings.ImapUser,
                    operation.Destination.Substring(queueIndex + 1),
                    $"NSB-MSG-{operation.Destination.Substring(0, queueIndex)}-{operation.Message.MessageId}",
                    serializedHeaders,
                    operation.Message.Body);

            }

            return Task.CompletedTask;
        }
    }
}
