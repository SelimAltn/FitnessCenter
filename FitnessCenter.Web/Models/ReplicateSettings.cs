namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// Replicate API ayarları
    /// Photo Mode için vücut dönüşümü görseli üretir
    /// </summary>
    public class ReplicateSettings
    {
        /// <summary>
        /// Replicate API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.replicate.com/v1";

        /// <summary>
        /// Replicate API Token
        /// </summary>
        public string ApiToken { get; set; } = "";

        /// <summary>
        /// API çağrısı için timeout (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;

        /// <summary>
        /// Kullanılacak model (SDXL img2img)
        /// </summary>
        public string ModelVersion { get; set; } = "stability-ai/sdxl:39ed52f2a78e934b3ba6e2a89f5b1c712de7dfea535525255b1aa35c5565e08b";

        /// <summary>
        /// API token yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(ApiToken) && ApiToken != "your-replicate-api-token-here";
    }
}
