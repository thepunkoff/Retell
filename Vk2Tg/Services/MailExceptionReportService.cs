using System.Net;
using System.Net.Mail;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vk2Tg.Abstractions.Services;
using Vk2Tg.Configuration;
namespace Vk2Tg.Services
{
    public class MailExceptionReportService : IExceptionReportService
    {
        private readonly SmtpClient _smtpClient;
        private readonly ILogger<MailExceptionReportService> _logger;
        private readonly IConfiguration _configuration;

        public MailExceptionReportService(SmtpClient smtpClient, ILogger<MailExceptionReportService> logger, IConfiguration configuration)
        {
            _smtpClient = smtpClient;
            _logger = logger;
            _configuration = configuration;
        }
        
        public async Task SendExceptionAsync(Exception exception)
        {
            var secrets = _configuration.Get<GmailSecrets>();
            
            _smtpClient.Credentials = new NetworkCredential(
                secrets.GmailEmail,
                secrets.GmailPassword);
            
            var sender = Assembly.GetExecutingAssembly().GetName().Name;

            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(secrets.GmailEmail),
                    Subject = "Vk2Tg exception",
                    Body = $"[{sender}]: {exception}",
                    To = { secrets.GmailEmail }
                };

                await _smtpClient.SendMailAsync(mail);
                
                _logger.LogTrace("Exception message has been reported");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in message sending");
            }
        }
    }
}