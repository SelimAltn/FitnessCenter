namespace FitnessCenter.Web.Models.Entities
{
    public class Egitmen
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = null!;
        public string? Uzmanlik { get; set; }
        public string? Biyografi { get; set; }

        public ICollection<EgitmenHizmet>? EgitmenHizmetler { get; set; }
        public ICollection<Musaitlik>? Musaitlikler { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }
    }
}
