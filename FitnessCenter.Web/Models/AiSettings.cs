namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// AI servis yapılandırması için options sınıfı.
    /// appsettings.json veya User Secrets'tan bind edilir.
    /// </summary>
    public class AiSettings
    {
        /// <summary>
        /// AI API endpoint URL'i (OpenAI uyumlu)
        /// </summary>
        public string Endpoint { get; set; } = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// API Key - User Secrets veya Environment Variable'dan gelecek
        /// appsettings.json'da boş bırakılmalı!
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Kullanılacak model adı
        /// </summary>
        public string Model { get; set; } = "gpt-4o-mini";

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
