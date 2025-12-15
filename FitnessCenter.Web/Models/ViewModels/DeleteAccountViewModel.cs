using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Hesap silme işlemi için şifre doğrulama ViewModel
    /// </summary>
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Şifre zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = null!;
    }
}
