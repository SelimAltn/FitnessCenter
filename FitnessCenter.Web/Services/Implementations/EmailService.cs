using FitnessCenter.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// SMTP üzerinden email gönderen servis
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly SmtpSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public bool IsConfigured => !string.IsNullOrEmpty(_settings.Host) && !string.IsNullOrEmpty(_settings.Username);

        public async Task<bool> SendAsync(string to, string subject, string body)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("SMTP ayarları yapılandırılmamış. Email gönderilemedi: {To}, Konu: {Subject}", to, subject);
                return false;
            }

            try
            {
                using var client = new SmtpClient(_settings.Host, _settings.Port)
                {
                    Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                    EnableSsl = true
                };

                var message = new MailMessage
                {
                    From = new MailAddress(_settings.FromEmail ?? _settings.Username, _settings.FromName ?? "Fitness Center"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                message.To.Add(to);

                await client.SendMailAsync(message);
                _logger.LogInformation("Email başarıyla gönderildi: {To}, Konu: {Subject}", to, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email gönderilirken hata oluştu: {To}, Konu: {Subject}", to, subject);
                return false;
            }
        }
    }

    /// <summary>
    /// SMTP ayarları için konfigürasyon sınıfı
    /// </summary>
    public class SmtpSettings
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
    }
}
