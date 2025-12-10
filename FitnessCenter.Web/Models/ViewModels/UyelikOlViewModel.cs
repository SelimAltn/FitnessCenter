using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    public class UyelikOlViewModel
    {
        [Required]
        [Display(Name = "Şube")]
        public int SalonId { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = null!;

        [Phone]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }
    }
}
