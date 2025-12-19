using FitnessCenter.Web.Models.ViewModels;

namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// DeepSeek AI servisi interface'i
    /// Metin tabanlı plan üretimi (OpenAI uyumlu API)
    /// </summary>
    public interface IDeepSeekService
    {
        /// <summary>
        /// Data modu: Kullanıcının ölçü bilgilerine göre plan üretir
        /// </summary>
        Task<AiResultVm> GetRecommendationAsync(AiRecommendVm input);

        /// <summary>
        /// Photo modu: Vision sonucuna göre plan üretir
        /// </summary>
        Task<AiResultVm> GetPhotoModeRecommendationAsync(VisionResult visionResult, AiRecommendVm preferences);

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        bool IsConfigured { get; }
    }
}
