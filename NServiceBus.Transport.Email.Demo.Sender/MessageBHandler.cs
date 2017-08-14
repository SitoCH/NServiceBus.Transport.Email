using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport.Email.Demo.Shared;

namespace NServiceBus.Transport.Email.Demo.Sender
{
    public class MessageBHandler : IHandleMessages<MessageB>
    {
        private static ILog _log = LogManager.GetLogger<MessageBHandler>();

        public Task Handle(MessageB message, IMessageHandlerContext context)
        {
            _log.Info("MessageB handled.");
            return Task.CompletedTask;
        }
    }
}
