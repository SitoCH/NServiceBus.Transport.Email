using System;

namespace NServiceBus.Transport.Email.Demo.Shared
{
    public class MessageA : IMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}