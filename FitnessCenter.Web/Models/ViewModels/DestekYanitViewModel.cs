using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Admin destek yanıtı için ViewModel
    /// </summary>
    public class DestekYanitViewModel
    {
        public int TicketId { get; set; }

        [Required(ErrorMessage = "Yanıt mesajı zorunludur.")]
        [StringLength(2000, ErrorMessage = "Yanıt en fazla 2000 karakter olabilir.")]
        [Display(Name = "Yanıtınız")]
        public string AdminCevap { get; set; } = null!;
    }
}
