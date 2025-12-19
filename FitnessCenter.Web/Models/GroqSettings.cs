namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// Groq API ayarları (Vision için)
    /// </summary>
    public class GroqSettings
    {
        /// <summary>
        /// Groq API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";

        /// <summary>
        /// API Key - User Secrets'tan gelecek
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Vision modeli (llama-3.2-11b-vision-preview önerilir)
        /// </summary>
        public string VisionModel { get; set; } = "llama-3.2-11b-vision-preview";

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
