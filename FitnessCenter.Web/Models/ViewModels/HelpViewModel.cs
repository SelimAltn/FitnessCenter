using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Yardım formu için ViewModel
    /// </summary>
    public class HelpViewModel
    {
        [Required(ErrorMessage = "Konu seçimi zorunludur.")]
        [Display(Name = "Konu")]
        public string Konu { get; set; } = null!;

        [Required(ErrorMessage = "Mesajınızı yazmanız gerekmektedir.")]
        [StringLength(2000, ErrorMessage = "Mesaj en fazla 2000 karakter olabilir.")]
        [Display(Name = "Mesajınız")]
        public string Mesaj { get; set; } = null!;

        [Required(ErrorMessage = "E-posta adresi zorunludur.")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
        [Display(Name = "İletişim E-postası")]
        public string Email { get; set; } = null!;
    }
}
