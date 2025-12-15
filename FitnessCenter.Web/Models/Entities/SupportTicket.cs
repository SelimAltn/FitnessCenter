using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Yardım/Destek talepleri için entity - İki yönlü iletişim destekli
    /// </summary>
    public class SupportTicket
    {
        public int Id { get; set; }

        #region Kullanıcı Bilgileri (Gönderen)

        /// <summary>
        /// Talebi oluşturan kullanıcı
        /// </summary>
        public string? UserId { get; set; }
        public ApplicationUser? User { get; set; }

        /// <summary>
        /// Kullanıcı adı (loglama için)
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Kullanıcı Adı")]
        public string? KullaniciAdi { get; set; }

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(200)]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        #endregion

        #region Destek Talebi İçeriği

        [Required(ErrorMessage = "Konu seçimi zorunludur.")]
        [StringLength(100, ErrorMessage = "Konu en fazla 100 karakter olabilir.")]
        [Display(Name = "Konu")]
        public string Konu { get; set; } = null!;

        [Required(ErrorMessage = "Mesaj zorunludur.")]
        [StringLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
        [Display(Name = "Mesaj")]
        public string Mesaj { get; set; } = null!;

        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.UtcNow;

        #endregion

        #region Durum ve Admin Yanıtı

        /// <summary>
        /// Ticket durumu: Open (Açık), Answered (Yanıtlandı), Closed (Kapalı)
        /// </summary>
        [StringLength(20)]
        [Display(Name = "Durum")]
        public string Durum { get; set; } = "Open";

        /// <summary>
        /// Admin'in yanıt mesajı
        /// </summary>
        [StringLength(2000)]
        [Display(Name = "Admin Yanıtı")]
        public string? AdminCevap { get; set; }

        /// <summary>
        /// Admin yanıt tarihi
        /// </summary>
        [Display(Name = "Yanıt Tarihi")]
        public DateTime? CevapTarihi { get; set; }

        /// <summary>
        /// Yanıtı veren admin'in ID'si
        /// </summary>
        public string? AdminId { get; set; }
        public ApplicationUser? Admin { get; set; }

        #endregion

        #region Mail Durumu

        /// <summary>
        /// Destek talebi admin'e mail olarak gönderildi mi?
        /// </summary>
        [Display(Name = "Admin'e Mail Gönderildi")]
        public bool AdminMailGonderildi { get; set; } = false;

        /// <summary>
        /// Admin yanıtı kullanıcıya mail olarak gönderildi mi?
        /// </summary>
        [Display(Name = "Kullanıcıya Mail Gönderildi")]
        public bool KullaniciMailGonderildi { get; set; } = false;

        #endregion
    }
}
