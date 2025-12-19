using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri formu için ViewModel
    /// İki mod: Fotoğraf VEYA Veri (mutual exclusive - ikisi aynı anda yok)
    /// </summary>
    public class AiRecommendVm : IValidatableObject
    {
        // ===== Ölçü Bilgileri (Data Modu) =====

        [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır.")]
        [Display(Name = "Boy (cm)")]
        public int? Boy { get; set; }

        [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır.")]
        [Display(Name = "Kilo (kg)")]
        public decimal? Kilo { get; set; }

        [Range(10, 100, ErrorMessage = "Yaş 10-100 arasında olmalıdır.")]
        [Display(Name = "Yaş")]
        public int? Yas { get; set; }

        [Display(Name = "Cinsiyet")]
        public string? Cinsiyet { get; set; }

        // ===== Fotoğraf (Photo Modu) =====

        [Display(Name = "Fotoğraf")]
        [DataType(DataType.Upload)]
        public IFormFile? Photo { get; set; }

        // ===== Tercihler (Her iki modda aktif) =====

        [Display(Name = "Hedef")]
        public string? Hedef { get; set; }

        [Display(Name = "Ekipman Durumu")]
        public string? Ekipman { get; set; }

        [Range(1, 7, ErrorMessage = "Antrenman günü 1-7 arasında olmalıdır.")]
        [Display(Name = "Haftalık Antrenman Günü")]
        public int? AntrenmanGunu { get; set; }

        // ===== Strict Mutual Exclusive Validation =====

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            bool hasPhoto = Photo != null && Photo.Length > 0;
            bool hasMeasurements = Boy.HasValue || Kilo.HasValue || Yas.HasValue || !string.IsNullOrEmpty(Cinsiyet);

            // KURAL: İkisi birden gelirse error
            if (hasPhoto && hasMeasurements)
            {
                yield return new ValidationResult(
                    "Fotoğraf ve ölçüler aynı anda gönderilemez. Lütfen birini seçin.",
                    new[] { nameof(Photo) });
            }

            // KURAL: En az biri olmalı
            if (!hasPhoto && !hasMeasurements)
            {
                yield return new ValidationResult(
                    "Lütfen ya fotoğraf yükleyin ya da ölçü bilgilerinizi girin.",
                    new[] { nameof(Photo), nameof(Boy) });
            }

            // Data modu seçilmişse: Boy ve Kilo zorunlu
            if (hasMeasurements && !hasPhoto)
            {
                if (!Boy.HasValue)
                {
                    yield return new ValidationResult(
                        "Boy bilgisi zorunludur.",
                        new[] { nameof(Boy) });
                }
                if (!Kilo.HasValue)
                {
                    yield return new ValidationResult(
                        "Kilo bilgisi zorunludur.",
                        new[] { nameof(Kilo) });
                }
            }
        }

        // ===== Helper Properties =====

        public bool IsPhotoMode => Photo != null && Photo.Length > 0;
        public bool IsDataMode => !IsPhotoMode && (Boy.HasValue || Kilo.HasValue);

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
            "Ekipman Yok (Evde)",
            "Temel Ekipman (Dambıl)",
            "Tam Donanımlı Salon"
        };
    }
}
