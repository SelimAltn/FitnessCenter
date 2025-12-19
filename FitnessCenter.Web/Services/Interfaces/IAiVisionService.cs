using FitnessCenter.Web.Models.ViewModels;

namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// AI Vision servisi interface'i
    /// Fotoğraf analizi için (Gemini Vision)
    /// </summary>
    public interface IAiVisionService
    {
        /// <summary>
        /// Fotoğrafı analiz eder
        /// - İnsan tespiti
        /// - Vücut sınıflandırması (Zayıf/Şişman/Kaslı)
        /// </summary>
        Task<VisionResult> AnalyzeAsync(byte[] imageBytes, string contentType);

        /// <summary>
        /// Servis yapılandırılmış mı?
        /// </summary>
        bool IsConfigured { get; }
    }
}
