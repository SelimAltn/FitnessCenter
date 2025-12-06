using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class AiLog
    {
        public int Id { get; set; }

        [Display(Name = "Üye")]
        public int? UyeId { get; set; }

        [Required(ErrorMessage = "Soru metni zorunludur.")]
        [StringLength(2000, ErrorMessage = "Soru en fazla 2000 karakter olabilir.")]
        [Display(Name = "Soru Metni")]
        public string SoruMetni { get; set; } = null!;

        [Required(ErrorMessage = "Cevap metni zorunludur.")]
        [StringLength(4000, ErrorMessage = "Cevap en fazla 4000 karakter olabilir.")]
        [Display(Name = "Cevap Metni")]
        public string CevapMetni { get; set; } = null!;

        [DataType(DataType.DateTime)]
        [Display(Name = "Oluşturulma Zamanı")]
        public DateTime OlusturulmaZamani { get; set; } = DateTime.UtcNow;

        [Display(Name = "Üye")]
        public Uye? Uye { get; set; }
    }
}
