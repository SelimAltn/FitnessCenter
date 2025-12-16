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

        #region Identity Bağlantısı

        /// <summary>
        /// Eğitmenin Identity kullanıcı bağlantısı (login için)
        /// </summary>
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        /// <summary>
        /// Eğitmenin kullanıcı adı (login için)
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Kullanıcı Adı")]
        public string? KullaniciAdi { get; set; }

        /// <summary>
        /// Şifre - Admin tarafından görülebilir şekilde saklanır
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Şifre")]
        public string? SifreHash { get; set; }

        #endregion

        #region İletişim Bilgileri

        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        #endregion

        #region Şube Bağlantısı (Tek Şube Kuralı)

        /// <summary>
        /// Eğitmenin çalıştığı şube
        /// </summary>
        [Display(Name = "Çalıştığı Şube")]
        public int? SalonId { get; set; }
        public Salon? Salon { get; set; }

        #endregion

        #region Finansal Bilgiler

        /// <summary>
        /// Eğitmenin maaşı
        /// </summary>
        [Display(Name = "Maaş (₺)")]
        [DataType(DataType.Currency)]
        [Range(0, 1000000, ErrorMessage = "Maaş 0 ile 1.000.000 arasında olmalıdır.")]
        public decimal? Maas { get; set; }

        #endregion

        #region Durum

        /// <summary>
        /// Eğitmen aktif mi? (Pasif eğitmenler randevu listelerinde görünmez)
        /// </summary>
        [Display(Name = "Aktif")]
        public bool Aktif { get; set; } = true;

        #endregion

        #region Biyografi

        [StringLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Biyografi")]
        public string? Biyografi { get; set; }

        #endregion

        #region İlişkiler

        /// <summary>
        /// Eğitmenin uzmanlık alanları (many-to-many)
        /// </summary>
        public ICollection<EgitmenUzmanlik>? EgitmenUzmanliklari { get; set; }

        public ICollection<EgitmenHizmet>? EgitmenHizmetler { get; set; }
        public ICollection<Musaitlik>? Musaitlikler { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }

        #endregion
    }
}
