using System;
using System.Threading.Tasks;

namespace NServiceBus.Transport.Email
{
    public class EmailTransportMessagePump : IPushMessages
    {
        public Task Init(Func<MessageContext, Task> onMessage, Func<ErrorContext, Task<ErrorHandleResult>> onError, CriticalError criticalError, PushSettings settings)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }

        public void Start(PushRuntimeSettings limitations)
        {
            //throw new NotImplementedException();
        }

        public Task Stop()
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }
    }
}