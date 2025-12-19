namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// AI servis yapılandırması için options sınıfı.
    /// DeepSeek API (OpenAI uyumlu) için yapılandırılmış.
    /// </summary>
    public class AiSettings
    {
        /// <summary>
        /// DeepSeek API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.deepseek.com";

        /// <summary>
        /// API Key - User Secrets veya Environment Variable'dan gelecek
        /// appsettings.json'da boş bırakılmalı!
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Kullanılacak model adı
        /// deepseek-chat: Genel sohbet/öneri
        /// deepseek-reasoner: Mantıksal düşünme gerektiren görevler
        /// </summary>
        public string Model { get; set; } = "deepseek-chat";

        /// <summary>
        /// API çağrı timeout süresi (saniye).
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
