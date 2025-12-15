using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Profil düzenleme için ViewModel (Email ve Şifre değiştirme)
    /// </summary>
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = null!;

        [Display(Name = "Kullanıcı Adı")]
        public string? UserName { get; set; }

        // Şifre değiştirme alanları (opsiyonel)
        
        [DataType(DataType.Password)]
        [Display(Name = "Mevcut Şifre")]
        public string? CurrentPassword { get; set; }

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        public string? NewPassword { get; set; }

        [Compare("NewPassword", ErrorMessage = "Şifreler uyuşmuyor.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        public string? ConfirmNewPassword { get; set; }
    }
}
