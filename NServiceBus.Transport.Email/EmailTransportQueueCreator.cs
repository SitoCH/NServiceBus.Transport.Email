using System;
using System.Threading.Tasks;

namespace NServiceBus.Transport.Email
{
    public class EmailTransportQueueCreator : ICreateQueues
    {
        public Task CreateQueueIfNecessary(QueueBindings queueBindings, string identity)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }
    }
}