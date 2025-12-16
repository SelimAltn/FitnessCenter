using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Admin eğitmen oluşturma formu için ViewModel
    /// </summary>
    public class EgitmenCreateVm
    {
        #region Identity / Kişisel Bilgiler

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = null!;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır.")]
        [Display(Name = "Kullanıcı Adı")]
        public string KullaniciAdi { get; set; } = null!;

        [Required(ErrorMessage = "Şifre zorunludur.")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [Display(Name = "Şifre")]
        public string Sifre { get; set; } = null!;

        #endregion

        #region Şube

        [Required(ErrorMessage = "Şube seçimi zorunludur.")]
        [Display(Name = "Çalışacağı Şube")]
        public int SalonId { get; set; }

        #endregion

        #region Uzmanlıklar

        [Display(Name = "Uzmanlık Alanları")]
        public List<int> SecilenUzmanliklar { get; set; } = new();

        #endregion

        #region Maaş

        [Display(Name = "Maaş (₺)")]
        [DataType(DataType.Currency)]
        [Range(0, 1000000, ErrorMessage = "Maaş 0 ile 1.000.000 arasında olmalıdır.")]
        public decimal? Maas { get; set; }

        #endregion

        #region Çalışma Saatleri

        public List<CalismaGunuVm> CalismaSaatleri { get; set; } = new();

        #endregion

        #region Biyografi

        [StringLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Biyografi")]
        [DataType(DataType.MultilineText)]
        public string? Biyografi { get; set; }

        #endregion
    }

    /// <summary>
    /// Haftalık çalışma günü için ViewModel
    /// </summary>
    public class CalismaGunuVm
    {
        public DayOfWeek Gun { get; set; }
        
        [Display(Name = "Çalışıyor")]
        public bool Calisiyor { get; set; }
        
        [Display(Name = "Başlangıç")]
        public TimeSpan? BaslangicSaati { get; set; }
        
        [Display(Name = "Bitiş")]
        public TimeSpan? BitisSaati { get; set; }

        /// <summary>
        /// Günün Türkçe adı
        /// </summary>
        public string GunAdi => Gun switch
        {
            DayOfWeek.Monday => "Pazartesi",
            DayOfWeek.Tuesday => "Salı",
            DayOfWeek.Wednesday => "Çarşamba",
            DayOfWeek.Thursday => "Perşembe",
            DayOfWeek.Friday => "Cuma",
            DayOfWeek.Saturday => "Cumartesi",
            DayOfWeek.Sunday => "Pazar",
            _ => Gun.ToString()
        };
    }

    /// <summary>
    /// Admin eğitmen düzenleme formu için ViewModel
    /// </summary>
    public class EgitmenEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad soyad zorunludur.")]
        [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = null!;

        [Required(ErrorMessage = "E-posta zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
        [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir.")]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [Display(Name = "Kullanıcı Adı")]
        public string? KullaniciAdi { get; set; }

        [Display(Name = "Şifre")]
        public string? Sifre { get; set; }

        [Required(ErrorMessage = "Şube seçimi zorunludur.")]
        [Display(Name = "Çalışacağı Şube")]
        public int SalonId { get; set; }

        [Display(Name = "Uzmanlık Alanları")]
        public List<int> SecilenUzmanliklar { get; set; } = new();

        [Display(Name = "Maaş (₺)")]
        [DataType(DataType.Currency)]
        [Range(0, 1000000, ErrorMessage = "Maaş 0 ile 1.000.000 arasında olmalıdır.")]
        public decimal? Maas { get; set; }

        public List<CalismaGunuVm> CalismaSaatleri { get; set; } = new();

        [StringLength(1000, ErrorMessage = "Biyografi en fazla 1000 karakter olabilir.")]
        [Display(Name = "Biyografi")]
        [DataType(DataType.MultilineText)]
        public string? Biyografi { get; set; }

        [Display(Name = "Aktif")]
        public bool Aktif { get; set; } = true;
    }
}
