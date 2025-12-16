using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Mesajlaşma sistemi için entity
    /// Trainer ↔ User ve Trainer ↔ Admin mesajlaşmalarını saklar
    /// </summary>
    public class Mesaj
    {
        public int Id { get; set; }

        /// <summary>
        /// Mesajı gönderen kullanıcı (ApplicationUser.Id)
        /// </summary>
        [Required]
        public string GonderenId { get; set; } = null!;
        public ApplicationUser Gonderen { get; set; } = null!;

        /// <summary>
        /// Mesajı alan kullanıcı (ApplicationUser.Id)
        /// </summary>
        [Required]
        public string AliciId { get; set; } = null!;
        public ApplicationUser Alici { get; set; } = null!;

        /// <summary>
        /// Mesaj içeriği
        /// </summary>
        [Required(ErrorMessage = "Mesaj içeriği zorunludur.")]
        [StringLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
        [Display(Name = "Mesaj")]
        public string Icerik { get; set; } = null!;

        /// <summary>
        /// Gönderim zamanı
        /// </summary>
        [Display(Name = "Gönderim Tarihi")]
        public DateTime GonderimTarihi { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Mesaj okundu mu?
        /// </summary>
        [Display(Name = "Okundu")]
        public bool Okundu { get; set; } = false;

        /// <summary>
        /// Konuşma tipi: "TrainerUser" | "TrainerAdmin"
        /// </summary>
        [StringLength(20)]
        public string? KonusmaTipi { get; set; }

        /// <summary>
        /// Trainer-User mesajlaşması için ilişkili randevu ID'si
        /// Mesajlaşmaya izin kuralı için kullanılır
        /// </summary>
        public int? RandevuId { get; set; }
        public Randevu? Randevu { get; set; }
    }
}
