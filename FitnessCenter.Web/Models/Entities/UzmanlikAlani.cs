using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Uzmanlık alanları tablosu (Fitness, Yoga, Pilates, vb.)
    /// </summary>
    public class UzmanlikAlani
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Uzmanlık adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Uzmanlık adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Uzmanlık Adı")]
        public string Ad { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        [Display(Name = "Aktif")]
        public bool Aktif { get; set; } = true;

        // Navigation
        public ICollection<EgitmenUzmanlik>? EgitmenUzmanliklari { get; set; }
    }
}
