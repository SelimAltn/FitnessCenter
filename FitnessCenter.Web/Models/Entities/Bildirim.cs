using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Kullanıcı bildirimleri için entity
    /// </summary>
    public class Bildirim
    {
        public int Id { get; set; }

        /// <summary>
        /// Bildirimin gönderildiği kullanıcı
        /// </summary>
        [Required]
        public string UserId { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Bildirim başlığı
        /// </summary>
        [Required]
        [StringLength(200)]
        [Display(Name = "Başlık")]
        public string Baslik { get; set; } = null!;

        /// <summary>
        /// Bildirim içeriği
        /// </summary>
        [Required]
        [StringLength(1000)]
        [Display(Name = "Mesaj")]
        public string Mesaj { get; set; } = null!;

        /// <summary>
        /// Bildirim türü: DestekYaniti, YeniUyelik, YeniRandevu, Sistem
        /// </summary>
        [Required]
        [StringLength(30)]
        [Display(Name = "Tür")]
        public string Tur { get; set; } = "Sistem";

        /// <summary>
        /// İlişkili kayıt ID'si (örn: TicketId, UyelikId, RandevuId)
        /// </summary>
        public int? IliskiliId { get; set; }

        /// <summary>
        /// Bildirim okundu mu?
        /// </summary>
        [Display(Name = "Okundu")]
        public bool Okundu { get; set; } = false;

        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        [Display(Name = "Tarih")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// İlgili sayfaya link
        /// </summary>
        [StringLength(200)]
        public string? Link { get; set; }
    }
}
