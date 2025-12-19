namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// Görsel üretim servisi interface'i
    /// "Nasıl görünürüm?" görseli için kullanılır
    /// </summary>
    public interface IImageGenerationService
    {
        /// <summary>
        /// Kullanıcının dönüşüm görselini üretir
        /// </summary>
        /// <param name="bodyCategory">Mevcut vücut kategorisi</param>
        /// <param name="targetGoal">Hedef (Kilo verme, Kas kazanma, Fit kalma)</param>
        /// <returns>Görsel URL'si veya null</returns>
        Task<string?> GenerateTransformationImageAsync(string bodyCategory, string targetGoal);

        /// <summary>
        /// Servis aktif mi?
        /// </summary>
        bool IsAvailable { get; }
    }
}
