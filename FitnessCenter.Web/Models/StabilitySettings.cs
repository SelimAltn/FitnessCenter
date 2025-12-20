namespace FitnessCenter.Web.Models
{
    /// <summary>
    /// Stability AI API ayarları (Image-to-Image için)
    /// </summary>
    public class StabilitySettings
    {
        /// <summary>
        /// Stability AI API base URL
        /// </summary>
        public string BaseUrl { get; set; } = "https://api.stability.ai";

        /// <summary>
        /// Model (stable-diffusion-xl-1024-v1-0)
        /// </summary>
        public string Model { get; set; } = "stable-diffusion-xl-1024-v1-0";

        /// <summary>
        /// API Key - User Secrets veya Environment Variable'dan gelecek
        /// Öncelik: StabilitySettings:ApiKey > STABILITY_API_KEY env var
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Timeout (saniye)
        /// </summary>
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Image Strength - düşük değer = orijinal görüntüye daha sadık (0.35-0.55 önerilir)
        /// </summary>
        public float Strength { get; set; } = 0.45f;

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey);
    }
}
