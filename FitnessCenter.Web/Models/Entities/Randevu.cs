using System;
using System.ComponentModel.DataAnnotations;
namespace FitnessCenter.Web.Models.Entities
{
    public class Randevu
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon seçimi zorunludur.")]
        [Display(Name = "Salon")]
        public int SalonId { get; set; }
        [Required(ErrorMessage = "Hizmet seçimi zorunludur.")]
        [Display(Name = "Hizmet")]
        public int HizmetId { get; set; }

        [Required(ErrorMessage = "Eğitmen seçimi zorunludur.")]
        [Display(Name = "Eğitmen")]
        public int EgitmenId { get; set; }

        [Required(ErrorMessage = "Üye seçimi zorunludur.")]
        [Display(Name = "Üye")]
        public int UyeId { get; set; }

        [Required(ErrorMessage = "Başlangıç zamanı zorunludur.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Başlangıç Zamanı")]
        public DateTime BaslangicZamani { get; set; }

        [Required(ErrorMessage = "Bitiş zamanı zorunludur.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Bitiş Zamanı")]
        public DateTime BitisZamani { get; set; }

        [StringLength(500, ErrorMessage = "Notlar en fazla 500 karakter olabilir.")]
        [Display(Name = "Notlar")]
        public string? Notlar { get; set; }

        [Required(ErrorMessage = "Randevu durumu zorunludur.")]
        [StringLength(20, ErrorMessage = "Durum en fazla 20 karakter olabilir.")]
        [Display(Name = "Durum")]
        public string Durum { get; set; } = "Beklemede";   // Beklemede / Onaylandı / İptal

        [Display(Name = "Salon")]
        public Salon? Salon { get; set; }

        [Display(Name = "Hizmet")]
        public Hizmet? Hizmet { get; set; }

        [Display(Name = "Eğitmen")]
        public Egitmen? Egitmen { get; set; }

        [Display(Name = "Üye")]
        public Uye? Uye { get; set; }
    }
}
