using System.Threading.Tasks;
using NServiceBus.Logging;
using NServiceBus.Transport.Email.Demo.Shared;

namespace NServiceBus.Transport.Email.Demo.Receiver
{
    public class MessageAHandler : IHandleMessages<MessageA>
    {
        private static ILog _log = LogManager.GetLogger<MessageAHandler>();

        public Task Handle(MessageA message, IMessageHandlerContext context)
        {
            _log.Info("MessageA Handled");
            _log.Info("Replying with MessageB");
            return context.Reply(new MessageB());
        }
    }
}
