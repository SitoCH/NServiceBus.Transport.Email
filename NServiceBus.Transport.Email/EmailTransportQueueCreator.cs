using System.Threading.Tasks;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email
{
    public class EmailTransportQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            ImapUtils.InitMailbox();
            return Task.CompletedTask;
        }
    }
}