namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// AI servis yapılandırması için options sınıfı.
    /// appsettings.json veya User Secrets'tan bind edilir.
    /// </summary>
    public class AiSettings
    {
        /// <summary>
        /// Gemini API base endpoint URL'i
        /// </summary>
        public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

        /// <summary>
        /// API Key - User Secrets veya Environment Variable'dan gelecek
        /// appsettings.json'da boş bırakılmalı!
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Kullanılacak Gemini model adı
        /// </summary>
        public string Model { get; set; } = "gemini-2.0-flash";

        /// <summary>
        /// API çağrı timeout süresi (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Cache geçerlilik süresi (saat)
        /// </summary>
        public int CacheHours { get; set; } = 24;

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
