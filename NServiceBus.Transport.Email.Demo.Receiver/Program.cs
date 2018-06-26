using System;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NServiceBus.Transport.Email.Demo.Shared;
using NServiceBus.Transport.Email.Utils;

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

            var demoSettings = ConsoleHelper.LoadDemoSettings();

            var endpointConfiguration = new EndpointConfiguration("NSB-Receiver-Endpoint");
            endpointConfiguration.UseSerialization<NewtonsoftSerializer>();
            endpointConfiguration.UseTransport<EmailTransport>()
                .GetSettings()
                .ConfigureEmailTransport(demoSettings);
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration);
            Console.WriteLine("Endpoint NSB-Receiver-Endpoint ready");
            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop();
        }
    }

}