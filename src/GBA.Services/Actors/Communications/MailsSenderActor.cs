using System;
using System.Globalization;
using System.IO;
using Akka.Actor;
using GBA.Common.Helpers;
using GBA.Domain.Messages.Communications.Mails;
using MailKit.Net.Smtp;
using MimeKit;
using MimeKit.Text;

namespace GBA.Services.Actors.Communications;

public sealed class MailsSenderActor : ReceiveActor {
    private const string SUCCESS_OPERATION = "Operation: SUCCESS \r\n Finished at {0} UTC \r\n\r\n";

    private const string FAILED_OPERATION =
        "Operation: FAILED \r\n Finished at {0} UTC \r\n Exception: \n {1} \r\n InnerException: \r\n {2} \r\n StackTrace: \r\n {3} \r\n\r\n";

    public MailsSenderActor() {
        Receive<SendNewECommerceOrderNotificationMessage>(message => {
            SendEmail(message.RecipientName, message.RecipientMail, message.Subject, message.Message);
        });
    }

    private void SendEmail(
        string recipientName,
        string recipientMail,
        string subject,
        string message) {
        try {
            MimeMessage emailMessage = new() {
                Subject = subject,
                Body = new TextPart(TextFormat.Html) {
                    Text = message
                }
            };

            emailMessage.From.Add(new MailboxAddress(ConfigurationManager.MailSenderName, ConfigurationManager.MailSenderMail));
            emailMessage.To.Add(new MailboxAddress(recipientName, recipientMail));

            using (SmtpClient client = new()) {
                client.ConnectAsync(ConfigurationManager.MailSmtpUrl, ConfigurationManager.MailPort, ConfigurationManager.MailRequireSsl).Wait();

                client.AuthenticateAsync(ConfigurationManager.MailUserName, ConfigurationManager.MailPassword).Wait();

                client.SendAsync(emailMessage).Wait();

                client.DisconnectAsync(true).Wait();
            }

            string logFilePath = Path.Combine(NoltFolderManager.GetLogFolderPath(), "mail_log.txt");

            string logData = string.Format(SUCCESS_OPERATION, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

            File.AppendAllText(logFilePath, logData);
        } catch (Exception exc) {
            string logFilePath = Path.Combine(NoltFolderManager.GetLogFolderPath(), "mail_error_log.txt");

            string logData = string.Format(FAILED_OPERATION, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture), exc.Message, exc.InnerException?.Message, exc.StackTrace);

            File.AppendAllText(logFilePath, logData);
        }
    }
}