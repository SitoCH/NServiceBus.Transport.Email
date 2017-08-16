﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus.Performance.TimeToBeReceived;
using NServiceBus.Routing;
using NServiceBus.Settings;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email
{
    public class EmailTransportInfrastructure : TransportInfrastructure
    {
        private readonly SettingsHolder _settings;

        public EmailTransportInfrastructure(SettingsHolder settings)
        {
            _settings = settings;
        }

        public override TransportReceiveInfrastructure ConfigureReceiveInfrastructure()
        {
            return new TransportReceiveInfrastructure(() => new EmailTransportMessagePump(_settings.EndpointName()), () => new EmailTransportQueueCreator(_settings.EndpointName()), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSendInfrastructure ConfigureSendInfrastructure()
        {
            return new TransportSendInfrastructure(() => new EmailDispatcher(), () => Task.FromResult(StartupCheckResult.Success));
        }

        public override TransportSubscriptionInfrastructure ConfigureSubscriptionInfrastructure()
        {
            throw new NotImplementedException();
        }

        public override EndpointInstance BindToLocalEndpoint(EndpointInstance instance)
        {
            return instance;
        }

        public override string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return string.IsNullOrEmpty(logicalAddress.Qualifier) ? $"{logicalAddress.EndpointInstance.Endpoint}@{ImapUtils.GetEmailUser()}" : $"{logicalAddress.EndpointInstance.Endpoint}.{logicalAddress.Qualifier}@{ImapUtils.GetEmailUser()}";
        }

        public override IEnumerable<Type> DeliveryConstraints
        {
            get
            {
                yield return typeof(DiscardIfNotReceivedBefore);
            }
        }

        public override TransportTransactionMode TransactionMode => TransportTransactionMode.ReceiveOnly;

        public override OutboundRoutingPolicy OutboundRoutingPolicy => new OutboundRoutingPolicy(OutboundRoutingType.Unicast, OutboundRoutingType.Unicast, OutboundRoutingType.Unicast);
    }
}
