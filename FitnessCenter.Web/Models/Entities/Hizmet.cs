namespace FitnessCenter.Web.Models.Entities
{
    public class Hizmet
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public int SureDakika { get; set; }          // örn: 60 dk
        public decimal Ucret { get; set; }           // isteğe göre
        public string? Aciklama { get; set; }

        public ICollection<EgitmenHizmet>? EgitmenHizmetler { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }
    }
}
