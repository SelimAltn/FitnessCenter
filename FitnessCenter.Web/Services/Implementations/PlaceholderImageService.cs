using FitnessCenter.Web.Services.Interfaces;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Placeholder görsel üretim servisi
    /// Gerçek implementasyon için DALL-E, Stability AI vb. eklenebilir
    /// </summary>
    public class PlaceholderImageService : IImageGenerationService
    {
        private readonly ILogger<PlaceholderImageService> _logger;

        public PlaceholderImageService(ILogger<PlaceholderImageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Servis şu an aktif değil
        /// </summary>
        public bool IsAvailable => false;

        /// <summary>
        /// Görsel üretimi - şu an devre dışı
        /// </summary>
        public Task<string?> GenerateTransformationImageAsync(string bodyCategory, string targetGoal)
        {
            _logger.LogInformation(
                "Image generation requested but service unavailable. Category: {Category}, Goal: {Goal}",
                bodyCategory, targetGoal);

            // Gerçek implementasyon eklendiğinde buraya API çağrısı gelecek
            return Task.FromResult<string?>(null);
        }
    }
}
