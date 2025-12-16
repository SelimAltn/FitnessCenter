using FitnessCenter.Web.Models.ViewModels;

namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// AI tabanlı fitness önerisi servisi interface'i
    /// </summary>
    public interface IAiRecommendationService
    {
        /// <summary>
        /// Kullanıcının ölçüleri ve tercihlerine göre AI önerisi üretir.
        /// Önce cache kontrol eder, cache varsa DB'den döner.
        /// </summary>
        /// <param name="input">Kullanıcı girdileri</param>
        /// <param name="uyeId">Üye ID</param>
        /// <returns>AI öneri sonucu</returns>
        Task<AiResultVm> GetRecommendationAsync(AiRecommendVm input, int uyeId);

        /// <summary>
        /// Input verilerinden SHA256 hash üretir (cache key)
        /// </summary>
        /// <param name="input">Kullanıcı girdileri</param>
        /// <param name="photoBytes">Opsiyonel foto byte array</param>
        /// <returns>SHA256 hash string</returns>
        string GenerateInputHash(AiRecommendVm input, byte[]? photoBytes = null);

        /// <summary>
        /// API yapılandırılmış mı kontrol eder
        /// </summary>
        bool IsApiConfigured { get; }

        // ===== AJAX Polling İçin Yeni Metodlar =====

        /// <summary>
        /// AI öneri işlemini arka planda başlatır.
        /// Hemen requestId döndürür, işlem arka planda devam eder.
        /// </summary>
        /// <param name="input">Kullanıcı girdileri</param>
        /// <param name="uyeId">Üye ID</param>
        /// <param name="requestId">Benzersiz istek kimliği</param>
        /// <returns>RequestId</returns>
        Task StartRecommendationAsync(AiRecommendVm input, int uyeId, string requestId);

        /// <summary>
        /// Belirtilen requestId için işlem durumunu sorgular.
        /// Polling ile çağrılır.
        /// </summary>
        /// <param name="requestId">İstek kimliği</param>
        /// <returns>Status (Pending/Processing/Completed/Error) ve sonuç (varsa)</returns>
        Task<(string Status, AiResultVm? Result, string? ErrorMessage)> GetRecommendationStatusAsync(string requestId);
    }
}

