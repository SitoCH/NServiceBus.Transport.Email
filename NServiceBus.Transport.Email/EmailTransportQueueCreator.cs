using System.Threading.Tasks;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email
{
    public class EmailTransportQueueCreator : ICreateQueues
    {
        private readonly string _endpointName;

        public EmailTransportQueueCreator(string endpointName)
        {
            _endpointName = endpointName;
        }

        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            ImapUtils.InitMailboxes(_endpointName);
            return Task.CompletedTask;
        }
    }
}