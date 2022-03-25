using System.Net;
using System.Net.Mail;
using System.Reflection;
using NLog;

namespace Vk2Tg
{
    public static class MailService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        
        private const string Mail = "mail";
        
        private static SmtpClient? _smtpClient;

        public static async Task SendException(Exception exception)
        {
            _smtpClient ??= _smtpClient = new SmtpClient("smtp.gmail.com")
            {
                EnableSsl = true,
                Port = 587,
                UseDefaultCredentials = false,
            };

            _smtpClient.Credentials = new NetworkCredential(
                Vk2TgConfig.Current.GmailEmail,
                Vk2TgConfig.Current.GmailPassword);
            
            var sender = Assembly.GetExecutingAssembly().GetName().Name;
#if DEBUG
            Logger.Error($"[DEBUG] Exception message from {sender} reported by mail:\n{exception}.");
            return;
#endif
            try
            {
                var mail = new MailMessage
                {
                    From = new MailAddress(Vk2TgConfig.Current.GmailEmail),
                    Subject = "Vk2Tg exception",
                    Body = $"[{sender}]: {exception}",
                    To = { Vk2TgConfig.Current.GmailEmail }
                };

                await _smtpClient.SendMailAsync(mail);
                
                Logger.Trace($"[{Mail}] exception message has been reported.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString());
            }
        }
    }
}