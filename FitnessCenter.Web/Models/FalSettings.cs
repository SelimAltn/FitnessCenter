namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// fal.ai API ayarları (Image-to-Image için)
    /// </summary>
    public class FalSettings
    {
        /// <summary>
        /// fal.ai API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://fal.run/fal-ai/fast-sdxl/image-to-image";

        /// <summary>
        /// API Key - User Secrets veya Environment Variable'dan gelecek
        /// Öncelik: FalSettings:ApiKey > FAL_API_KEY env var
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Timeout (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Strength - düşük değer = orijinal görüntüye daha sadık (0.35-0.55 önerilir)
        /// </summary>
        public float Strength { get; set; } = 0.45f;

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
