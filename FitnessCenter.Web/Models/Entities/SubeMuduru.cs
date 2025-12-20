using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Sube Muduru (Branch Manager) entity.
    /// Each salon can have at most one manager.
    /// </summary>
    public class SubeMuduru
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [StringLength(100)]
        [Display(Name = "Ad Soyad")]
        public string AdSoyad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email zorunludur.")]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20)]
        [Display(Name = "Telefon")]
        public string? Telefon { get; set; }

        [Display(Name = "Aktif")]
        public bool Aktif { get; set; } = true;

        // Link to Identity user for login
        [Display(Name = "Kullanici Hesabi")]
        public string? ApplicationUserId { get; set; }
        public ApplicationUser? ApplicationUser { get; set; }

        // Link to Salon (1-to-1)
        [Required(ErrorMessage = "Sube secimi zorunludur.")]
        [Display(Name = "Sube")]
        public int SalonId { get; set; }
        public Salon? Salon { get; set; }

        [Display(Name = "Olusturulma Tarihi")]
        public DateTime OlusturulmaTarihi { get; set; } = DateTime.Now;
    }
}
