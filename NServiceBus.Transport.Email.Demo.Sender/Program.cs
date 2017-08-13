using System;
using System.Threading.Tasks;
using NServiceBus.Features;
using NServiceBus.Transport.Email.Demo.Shared;

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
            var endpointConfiguration = new EndpointConfiguration("SenderEndpointName");
            endpointConfiguration.UseTransport<EmailTransport>();
            
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            var receiverName = "ReceiverEndpointName@sito@grignola.ch";
            var messageA = new MessageA();
            await endpointInstance.Send(receiverName, messageA).ConfigureAwait(false);

            Console.WriteLine("MessageA sent to endpoint {0}. Press any key to exit", receiverName);
            Console.ReadKey();
            await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
