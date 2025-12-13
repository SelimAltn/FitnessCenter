namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri sonucu için ViewModel
    /// </summary>
    public class AiResultVm
    {
        /// <summary>
        /// Kısa özet (2-3 cümle)
        /// </summary>
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Antrenman planı maddeleri
        /// </summary>
        public List<string> WorkoutPlan { get; set; } = new();

        /// <summary>
        /// Beslenme önerileri
        /// </summary>
        public List<string> NutritionTips { get; set; } = new();

        /// <summary>
        /// Uyarılar ve dikkat edilmesi gerekenler
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Sonuç cache'den mi geldi?
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Sonucun üretildiği tarih
        /// </summary>
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Kullanıcının girdiği ölçüler (özet)
        /// </summary>
        public string InputSummary { get; set; } = string.Empty;

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Fallback yanıt mı? (API key yoksa)
        /// </summary>
        public bool IsFallback { get; set; }
    }
}
