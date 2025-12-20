namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// OpenAI Image API ayarları
    /// Photo Mode için vücut dönüşümü görseli üretir (DALL-E 3 veya gpt-image-1)
    /// </summary>
    public class OpenAIImageSettings
    {
        /// <summary>
        /// OpenAI API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.openai.com/v1";

        /// <summary>
        /// OpenAI API Key
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Kullanılacak model (dall-e-3 veya gpt-image-1)
        /// </summary>
        public string Model { get; set; } = "gpt-image-1";

        /// <summary>
        /// Görsel boyutu
        /// </summary>
        public string Size { get; set; } = "1024x1024";

        /// <summary>
        /// API çağrısı için timeout (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// API key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && ApiKey.StartsWith("sk-");
    }
}
