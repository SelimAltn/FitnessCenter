namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Vision servisi analiz sonucu
    /// Fotoğraftan insan tespiti ve vücut sınıflandırması
    /// </summary>
    public class VisionResult
    {
        /// <summary>
        /// Fotoğrafta insan var mı?
        /// </summary>
        public bool IsHuman { get; set; }

        /// <summary>
        /// Vücut kategorisi (sadece insan varsa)
        /// Zayıf | Şişman | Kaslı | Belirsiz
        /// </summary>
        public string BodyCategory { get; set; } = "Belirsiz";

        /// <summary>
        /// Fotoğraf açıklaması (1-2 cümle)
        /// İnsan varsa: görünüm açıklaması
        /// İnsan yoksa: ne olduğunu açıklar
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
