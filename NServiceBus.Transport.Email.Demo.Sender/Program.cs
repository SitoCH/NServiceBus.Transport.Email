using System;
using System.Configuration;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvanceExtensibility;
using NServiceBus.Features;
using NServiceBus.Transport.Email.Demo.Shared;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email.Demo.Sender
{
    class Program
    {
        static void Main()
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        private static async Task AsyncMain()
        {
            Console.Title = "NServiceBus.Transport.Email.Demo.Sender";
            var endpointConfiguration = new EndpointConfiguration(ConfigurationManager.AppSettings[Constants.ENDPOINT_NAME].Replace("@", "_"));
            endpointConfiguration.UseTransport<EmailTransport>();

            InitSettings(endpointConfiguration);

            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            var receiverName = ConfigurationManager.AppSettings["receiver.name"];
            var messageA = new MessageA();
            await endpointInstance.Send(receiverName, messageA).ConfigureAwait(false);

            Console.WriteLine("MessageA sent to endpoint {0}. Press any key to exit", receiverName);
            Console.ReadKey();
            await endpointInstance.Stop().ConfigureAwait(false);
        }

        private static void InitSettings(EndpointConfiguration endpointConfiguration)
        {
            endpointConfiguration.GetSettings().Set(Constants.ENDPOINT_NAME, ConfigurationManager.AppSettings[Constants.ENDPOINT_NAME]);
            endpointConfiguration.GetSettings().Set(Constants.MAIL_HOST, ConfigurationManager.AppSettings[Constants.MAIL_HOST]);
            endpointConfiguration.GetSettings().Set(Constants.MAIL_HOST_PORT, int.Parse(ConfigurationManager.AppSettings[Constants.MAIL_HOST_PORT]));
            endpointConfiguration.GetSettings().Set(Constants.SMTP_HOST, ConfigurationManager.AppSettings[Constants.SMTP_HOST]);
            endpointConfiguration.GetSettings().Set(Constants.SMTP_HOST_PORT, int.Parse(ConfigurationManager.AppSettings[Constants.SMTP_HOST_PORT]));
            endpointConfiguration.GetSettings().Set(Constants.SMTP_USER, ConfigurationManager.AppSettings[Constants.SMTP_USER]);
            endpointConfiguration.GetSettings().Set(Constants.SMTP_PASSWORD, ConfigurationManager.AppSettings[Constants.SMTP_PASSWORD]);
        }
    }
}
