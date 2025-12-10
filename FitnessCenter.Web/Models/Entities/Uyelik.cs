using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class Uyelik
    {
        public int Id { get; set; }

        [Required]
        public int UyeId { get; set; }
        public Uye Uye { get; set; } = null!;

        [Required]
        public int SalonId { get; set; }
        public Salon Salon { get; set; } = null!;

        [Required]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime BaslangicTarihi { get; set; }

        [Display(Name = "Bitiş Tarihi")]
        public DateTime? BitisTarihi { get; set; }

        [Required]
        [StringLength(20)]
        public string Durum { get; set; } = "Aktif"; // Aktif / Donduruldu / İptal vb.

        // İleride paket tipi vs. eklemek istersen buraya koyarsın
        // public string? PaketTuru { get; set; }
    }
}
