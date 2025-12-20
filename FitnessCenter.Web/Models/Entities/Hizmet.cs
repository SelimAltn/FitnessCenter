using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class Hizmet
    {
        public int Id { get; set; }


        [Required(ErrorMessage = "Hizmet adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Hizmet adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Hizmet Adı")]
        public string Ad { get; set; } = null!;


        [Required(ErrorMessage = "Sure zorunludur.")]
        [Range(10, 300, ErrorMessage = "Sure 10 ile 300 dakika arasinda olmalidir.")]
        [Display(Name = "Sure (Dakika)")]
        public int SureDakika { get; set; }




        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }



        public ICollection<EgitmenHizmet>? EgitmenHizmetler { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }
    }
}
