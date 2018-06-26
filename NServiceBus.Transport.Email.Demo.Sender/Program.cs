using System;
using System.Configuration;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Transport.Email.Demo.Shared;
using NServiceBus.Transport.Email.Utils;

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
            
            var demoSettings = ConsoleHelper.LoadDemoSettings();
            
            var endpointConfiguration = new EndpointConfiguration("NSB-Sender-Endpoint");
            endpointConfiguration.UseTransport<EmailTransport>().GetSettings().ConfigureEmailTransport(demoSettings);
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            
            var endpointInstance = await Endpoint.Start(endpointConfiguration);

            var messageA = new MessageA();
            var receiverEndpointName = $"NSB-Receiver-Endpoint@{demoSettings.ImapUser}";
            
            await endpointInstance.Send(receiverEndpointName, messageA);
            
            
            Console.WriteLine("MessageA sent to endpoint {0}. Press any key to exit", receiverEndpointName);
            Console.ReadKey();
            await endpointInstance.Stop();
        }
    }
}