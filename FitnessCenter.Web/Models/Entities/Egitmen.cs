using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class Egitmen
    {
        public int Id { get; set; }


        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = null!;


        [StringLength(150, ErrorMessage = "Uzmanlık alanı en fazla 150 karakter olabilir.")]
        [Display(Name = "Uzmanlık")]
        public string? Uzmanlik { get; set; }

        [StringLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Biyografi")]
        public string? Biyografi { get; set; }

        public ICollection<EgitmenHizmet>? EgitmenHizmetler { get; set; }
        public ICollection<Musaitlik>? Musaitlikler { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }
    }
}
