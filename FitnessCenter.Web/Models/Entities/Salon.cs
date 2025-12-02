namespace FitnessCenter.Web.Models.Entities
{
    public class Salon
    {
        public int Id { get; set; }
        public string Ad { get; set; } = null!;
        public string ? Adress { get; set; }
        public string? Aciklama { get; set; }
        public ICollection<Randevu>? Randevular { get; set; }


    }
}
