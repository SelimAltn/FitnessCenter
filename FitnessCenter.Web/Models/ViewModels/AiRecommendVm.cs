using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri formu için ViewModel
    /// Fotoğraf VEYA ölçü bilgileri (Boy+Kilo+Yaş) girilmelidir.
    /// </summary>
    public class AiRecommendVm : IValidatableObject
    {
        [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır.")]
        [Display(Name = "Boy (cm) - Opsiyonel")]
        public int? Boy { get; set; }

        [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır.")]
        [Display(Name = "Kilo (kg) - Opsiyonel")]
        public decimal? Kilo { get; set; }

        [Range(10, 100, ErrorMessage = "Yaş 10-100 arasında olmalıdır.")]
        [Display(Name = "Yaş - Opsiyonel")]
        public int? Yas { get; set; }

        [Display(Name = "Cinsiyet")]
        public string? Cinsiyet { get; set; }

        [Display(Name = "Hedef")]
        public string? Hedef { get; set; }

        [Range(1, 7, ErrorMessage = "Haftalık antrenman günü 1-7 arasında olmalıdır.")]
        [Display(Name = "Haftalık Antrenman Günü")]
        public int? AntrenmanGunu { get; set; }

        [Display(Name = "Ekipman Durumu")]
        public string? Ekipman { get; set; }

        [StringLength(500, ErrorMessage = "Sağlık kısıtı en fazla 500 karakter olabilir.")]
        [Display(Name = "Sağlık Kısıtları (Opsiyonel)")]
        public string? SaglikKisiti { get; set; }

        [Display(Name = "Fotoğraf (Opsiyonel)")]
        [DataType(DataType.Upload)]
        public IFormFile? Photo { get; set; }

        // ===== Custom Validation =====

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasPhoto = Photo != null && Photo.Length > 0;
            bool hasMeasurements = Boy.HasValue && Kilo.HasValue && Yas.HasValue;

            if (!hasPhoto && !hasMeasurements)
            {
                yield return new ValidationResult(
                    "Lütfen fotoğraf yükleyin VEYA ölçü bilgilerini (Boy, Kilo, Yaş) girin.",
                    new[] { nameof(Photo), nameof(Boy), nameof(Kilo), nameof(Yas) });
            }

            // Kısmi ölçü girilmişse uyarı
            if (!hasPhoto && (Boy.HasValue || Kilo.HasValue || Yas.HasValue) && !hasMeasurements)
            {
                yield return new ValidationResult(
                    "Fotoğraf olmadan öneri almak için Boy, Kilo ve Yaş bilgilerinin tamamı girilmelidir.",
                    new[] { nameof(Boy), nameof(Kilo), nameof(Yas) });
            }
        }

        // ===== Helper Properties =====

        /// <summary>
        /// Giriş senaryosunu belirler: PhotoOnly, DataOnly, Combined
        /// </summary>
        public string GetInputScenario()
        {
            bool hasPhoto = Photo != null && Photo.Length > 0;
            bool hasMeasurements = Boy.HasValue && Kilo.HasValue && Yas.HasValue;

            if (hasPhoto && hasMeasurements)
                return "Combined";
            if (hasPhoto)
                return "PhotoOnly";
            return "DataOnly";
        }

        // ===== Seçenek Listeleri (View için) =====

        public static List<string> HedefSecenekleri => new()
        {
            "Kilo Verme",
            "Kas Kazanma",
            "Fit Kalma"
        };

        public static List<string> CinsiyetSecenekleri => new()
        {
            "Erkek",
            "Kadın",
            "Belirtmek İstemiyorum"
        };

        public static List<string> EkipmanSecenekleri => new()
        {
            "Bodyweight (Alet yok)",
            "Dumbbell (Evde ağırlık)",
            "Gym (Salon erişimi)"
        };
    }
}
