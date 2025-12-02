namespace FitnessCenter.Web.Models.Entities
{
    public class Randevu
    {
        public int Id { get; set; }

        public int SalonId { get; set; }
        public int HizmetId { get; set; }
        public int EgitmenId { get; set; }
        public int UyeId { get; set; }

        public DateTime BaslangicZamani { get; set; }
        public DateTime BitisZamani { get; set; }

        public string? Notlar { get; set; }
        public string Durum { get; set; } = "Beklemede";   // Beklemede / Onaylandı / İptal

        public Salon? Salon { get; set; }
        public Hizmet? Hizmet { get; set; }
        public Egitmen? Egitmen { get; set; }
        public Uye? Uye { get; set; }
    }
}
