using System;
using System.IO;
using Newtonsoft.Json;
using NServiceBus.Transport.Email.Utils;

namespace NServiceBus.Transport.Email.Demo.Shared
{
    public static class ConsoleHelper
    {
        private const string _settingsFilePath = "demo-settings.json";


        public static DemoSettings LoadDemoSettings()
        {
            if (File.Exists(_settingsFilePath))
            {
                return JsonConvert.DeserializeObject<DemoSettings>(File.ReadAllText(_settingsFilePath));
            }

            // IMAP
            Console.Write("IMAP server: ");
            var imapServer = Console.ReadLine();
            Console.Write("IMAP server port: ");
            var imapServerPort = Console.ReadLine();
            Console.Write("IMAP user: ");
            var imapUser = Console.ReadLine();
            Console.Write("IMAP password: ");
            var imapPassword = Console.ReadLine();

            // Global
            Console.Write("SMTP server: ");
            var smtpServer = Console.ReadLine();
            Console.Write("SMTP server port: ");
            var smtpServerPort = Console.ReadLine();
            Console.Write("SMTP user: ");
            var smtpUser = Console.ReadLine();
            Console.Write("SMTP password: ");
            var smtpPassword = Console.ReadLine();

            var settings = new DemoSettings
            {
                ImapServer = imapServer,
                ImapServerPort = int.Parse(imapServerPort),
                ImapUser = imapUser,
                ImapPassword = imapPassword,
                SmtpServer = smtpServer,
                SmtpServerPort = int.Parse(smtpServerPort),
                SmtpUser = smtpUser,
                SmtpPassword = smtpPassword,
            };

            File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(settings));

            return settings;
        }
    }

    public class DemoSettings : IEmailTransportSettings
    {
        public string ImapServer { get; set; }

        public int ImapServerPort { get; set; }

        public string ImapUser { get; set; }

        public string ImapPassword { get; set; }

        public string SmtpServer { get; set; }

        public int SmtpServerPort { get; set; }

        public string SmtpUser { get; set; }

        public string SmtpPassword { get; set; }
    }
}