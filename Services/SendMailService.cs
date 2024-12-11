using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;


namespace webMVC.Services
{

    public class MailSettings
    {
        public string? Mail { get; set; }
        public string? DisplayName { get; set; }
        public string? Password { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
    }


    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message);
    }


    public class SendMailService : IEmailSender
    {
        private readonly MailSettings _mailSettings;
        private readonly ILogger<SendMailService> _logger;

        public SendMailService(IOptions<MailSettings> mailSettings, ILogger<SendMailService> logger)
        {
            _mailSettings = mailSettings.Value;
            _logger = logger;
            _logger.LogInformation("Create SendMailService");
        }


        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {

            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email cannot be null or empty", nameof(email));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("Subject cannot be null or empty", nameof(subject));
            if (string.IsNullOrWhiteSpace(htmlMessage)) throw new ArgumentException("Message body cannot be null or empty", nameof(htmlMessage));

            var message = new MimeMessage();
            message.Sender = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail);
            message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlMessage
            };
            message.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            try
            {
                _logger.LogInformation("Connecting to SMTP server...");
                smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
                _logger.LogInformation("Authenticating...");
                smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
                _logger.LogInformation("Sending email...");
                await smtp.SendAsync(message);
                _logger.LogInformation($"Email sent successfully to {email}.");
            }
            catch (Exception ex)
            {
                Directory.CreateDirectory("MailsSave");
                var savePath = Path.Combine("MailsSave", $"{Guid.NewGuid()}.eml");
                await message.WriteToAsync(savePath);

                _logger.LogError($"Failed to send email. Saved email to {savePath}. Error: {ex.Message}");
                throw;
            }
            finally
            {
                if (smtp.IsConnected)
                {
                    await smtp.DisconnectAsync(true);
                    _logger.LogInformation("SMTP connection closed.");
                }
            }
        }
    }
}