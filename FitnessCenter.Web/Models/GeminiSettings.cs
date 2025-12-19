namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// Gemini API ayarları
    /// Vision servisi için kullanılır
    /// </summary>
    public class GeminiSettings
    {
        /// <summary>
        /// Gemini API base URL
        /// </summary>
        public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models";

        /// <summary>
        /// API Key - User Secrets'tan gelecek
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Vision modeli
        /// </summary>
        public string VisionModel { get; set; } = "gemini-1.5-flash-latest";

        /// <summary>
        /// Timeout (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
