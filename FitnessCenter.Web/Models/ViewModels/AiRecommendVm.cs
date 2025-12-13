using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri formu için ViewModel
    /// </summary>
    public class AiRecommendVm
    {
        [Required(ErrorMessage = "Boy bilgisi zorunludur.")]
        [Range(100, 250, ErrorMessage = "Boy 100-250 cm arasında olmalıdır.")]
        [Display(Name = "Boy (cm)")]
        public int Boy { get; set; }

        [Required(ErrorMessage = "Kilo bilgisi zorunludur.")]
        [Range(30, 300, ErrorMessage = "Kilo 30-300 kg arasında olmalıdır.")]
        [Display(Name = "Kilo (kg)")]
        public decimal Kilo { get; set; }

        [Required(ErrorMessage = "Yaş bilgisi zorunludur.")]
        [Range(10, 100, ErrorMessage = "Yaş 10-100 arasında olmalıdır.")]
        [Display(Name = "Yaş")]
        public int Yas { get; set; }

        [Display(Name = "Cinsiyet")]
        public string? Cinsiyet { get; set; }

        [Required(ErrorMessage = "Hedef seçimi zorunludur.")]
        [Display(Name = "Hedef")]
        public string Hedef { get; set; } = null!;

        [Required(ErrorMessage = "Haftalık antrenman günü zorunludur.")]
        [Range(1, 7, ErrorMessage = "Haftalık antrenman günü 1-7 arasında olmalıdır.")]
        [Display(Name = "Haftalık Antrenman Günü")]
        public int AntrenmanGunu { get; set; }

        [Required(ErrorMessage = "Ekipman seçimi zorunludur.")]
        [Display(Name = "Ekipman Durumu")]
        public string Ekipman { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Sağlık kısıtı en fazla 500 karakter olabilir.")]
        [Display(Name = "Sağlık Kısıtları (Opsiyonel)")]
        public string? SaglikKisiti { get; set; }

        [Display(Name = "Fotoğraf (Opsiyonel)")]
        [DataType(DataType.Upload)]
        public IFormFile? Photo { get; set; }

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
