using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class Musaitlik
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Eğitmen")]
        public int EgitmenId { get; set; }

        [Required]
        [Display(Name = "Gün")]
        public DayOfWeek Gun { get; set; }          // Pazartesi, Salı vs.

        [Required]
        [Display(Name = "Başlangıç Saati")]
        public TimeSpan BaslangicSaati { get; set; }

        [Required]
        [Display(Name = "Bitiş Saati")]
        public TimeSpan BitisSaati { get; set; }

        public Egitmen? Egitmen { get; set; }
    }
}
