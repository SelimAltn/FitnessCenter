using System.ComponentModel.DataAnnotations;
namespace FitnessCenter.Web.Models.Entities
{
    public class Uye
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100,ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display (Name = "AdSoyad")]
        public string AdSoyad { get; set; } = null!;

        [Required(ErrorMessage = " E-posta zorunludur.")]
        [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        [EmailAddress(ErrorMessage = "Geçerli bir E-posta giriniz")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Phone (ErrorMessage ="Geçerli bir telefon numarası giriniz")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        public ICollection<Randevu>? Randevular { get; set; }
        public ICollection<AiLog>? AiLoglar { get; set; }
    }
}
