using System;
using System.Threading.Tasks;
using NServiceBus.Configuration.AdvanceExtensibility;
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
            var endpointConfiguration = new EndpointConfiguration("test");
            endpointConfiguration.UseTransport<EmailTransport>();
            endpointConfiguration.GetSettings().Set("mail.from", "test@mail.com");
            endpointConfiguration.GetSettings().Set("mail.host", "smtp.mail.com");
            endpointConfiguration.GetSettings().Set("mail.port", 123);

            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.DisableFeature<TimeoutManager>();

            var endpointInstance = await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);

            var messageA = new MessageA();
            await endpointInstance.Send("Samples.CustomTransport.Endpoint2", messageA).ConfigureAwait(false);

            Console.WriteLine("MessageA sent. Press any key to exit");
            Console.ReadKey();
            await endpointInstance.Stop().ConfigureAwait(false);
        }
    }
}
