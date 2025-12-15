namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// Email gönderimi için servis interface
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Email gönderir
        /// </summary>
        /// <param name="to">Alıcı email</param>
        /// <param name="subject">Konu</param>
        /// <param name="body">İçerik (HTML)</param>
        /// <returns>Başarılı ise true</returns>
        Task<bool> SendAsync(string to, string subject, string body);

        /// <summary>
        /// Email servisi yapılandırılmış mı?
        /// </summary>
        bool IsConfigured { get; }
    }
}
