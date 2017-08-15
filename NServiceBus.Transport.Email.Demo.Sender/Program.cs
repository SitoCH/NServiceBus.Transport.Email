using System;
using System.Configuration;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Transport.Email.Demo.Shared;

namespace NServiceBus.Transport.Email.Demo.Sender
{
    internal class Program
    {
        private static void Main()
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        private static async Task AsyncMain()
        {
            Console.Title = "NServiceBus.Transport.Email.Demo.Sender";
            var endpointConfiguration = new EndpointConfiguration("SenderEndpointName");
            endpointConfiguration.UseTransport<EmailTransport>();

            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            var messageA = new MessageA();
            var receiverEndpointName = ConfigurationManager.AppSettings["ReceiverEndpointName"];
            await endpointInstance.Send(receiverEndpointName, messageA).ConfigureAwait(false);

            Console.WriteLine("MessageA sent to endpoint {0}. Press any key to exit", receiverEndpointName);
            Console.ReadKey();
            await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
