using System;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.Transport.Email;
using NServiceBus.Transport.Email.Utils;
using NServiceBus.TransportTests;


public class ConfigureEmailTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportConfigurationResult Configure(SettingsHolder settings, TransportTransactionMode transactionMode)
    {
        return new TransportConfigurationResult
        {
            PurgeInputQueueOnStartup = true,
            TransportInfrastructure = new EmailTransportInfrastructure(settings.ConfigureEmailTransport(new EnvironmentSettings()))
        };
    }

    public Task Cleanup()
    {
        return Task.CompletedTask;
    }
}

internal class EnvironmentSettings : IEmailTransportSettings
{
    public string ImapServer => Environment.GetEnvironmentVariable("NSB_IMAP_SERVER");
    public int ImapServerPort => int.Parse(Environment.GetEnvironmentVariable("NSB_IMAP_SERVER_PORT"));
    public string ImapUser => Environment.GetEnvironmentVariable("NSB_EMAIL_USER");
    public string ImapPassword => Environment.GetEnvironmentVariable("NSB_EMAIL_PASSWORD");
    public string SmtpServer => Environment.GetEnvironmentVariable("NSB_SMTP_SERVER");
    public int SmtpServerPort => int.Parse(Environment.GetEnvironmentVariable("NSB_SMTP_SERVER_PORT"));
    public string SmtpUser => Environment.GetEnvironmentVariable("NSB_EMAIL_USER");
    public string SmtpPassword => Environment.GetEnvironmentVariable("NSB_EMAIL_PASSWORD");
}