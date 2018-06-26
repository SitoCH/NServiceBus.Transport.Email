using NServiceBus.Settings;

namespace NServiceBus.Transport.Email
{
    public class EmailTransport : TransportDefinition
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            return new EmailTransportInfrastructure(settings);
        }

        public override bool RequiresConnectionString => false;

        public override string ExampleConnectionStringForErrorMessage => "";
    }
}
