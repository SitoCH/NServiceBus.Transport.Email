using MailKit.Net.Imap;
using MailKit.Security;
using NServiceBus.Settings;

namespace NServiceBus.Transport.Email.Utils
{
    public static class SettingsHolderExtensions
    {
        public static IEmailTransportSettings getTransportSettings(this SettingsHolder settingsHolder)
        {
            return settingsHolder.Get<IEmailTransportSettings>("emailTransportSettings");
        }

        public static ImapClient GetImapClient(this SettingsHolder settingsHolder)
        {
            var settings = settingsHolder.getTransportSettings();

            // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
            var client = new ImapClient {ServerCertificateValidationCallback = (s, c, h, e) => true};

            client.Connect(settings.ImapServer, settings.ImapServerPort, SecureSocketOptions.SslOnConnect);
            client.AuthenticationMechanisms.Remove("XOAUTH2");
            client.Authenticate(settings.ImapUser, settings.ImapPassword);

            return client;
        }

        public static SettingsHolder ConfigureEmailTransport(this SettingsHolder settingsHolder, IEmailTransportSettings settings)
        {
            settingsHolder.Set("emailTransportSettings", settings);
            return settingsHolder;
        }
    }

    public interface IEmailTransportSettings
    {
        string ImapServer { get; }
        int ImapServerPort { get; }
        string ImapUser { get; }
        string ImapPassword { get; }
        string SmtpServer { get; }
        int SmtpServerPort { get; }
        string SmtpUser { get; }
        string SmtpPassword { get; }
    }
}