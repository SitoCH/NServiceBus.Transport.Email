using System;
using System.Threading.Tasks;
using NServiceBus.Features;

namespace NServiceBus.Transport.Email.Demo.Receiver
{
    internal class Program
    {
        private static void Main()
        {
            AsyncMain().GetAwaiter().GetResult();
        }

        private static async Task AsyncMain()
        {
            Console.Title = "NServiceBus.Transport.Email.Demo.Receiver";

            var endpointConfiguration = new EndpointConfiguration("ReceiverEndpointName");
            endpointConfiguration.UseTransport<EmailTransport>();
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
