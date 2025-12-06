using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class EgitmenHizmet
    {

        [Required(ErrorMessage = "Eğitmen seçimi zorunludur.")]
        [Display(Name = "Eğitmen")]
        public int EgitmenId { get; set; }

        [Required(ErrorMessage = "Hizmet seçimi zorunludur.")]
        [Display(Name = "Hizmet")]
        public int HizmetId { get; set; }

        [Display(Name = "Eğitmen")]
        public Egitmen? Egitmen { get; set; }

        [Display(Name = "Hizmet")]
        public Hizmet? Hizmet { get; set; }
    }
}
