namespace FitnessCenter.Web.Models.Entities
{
    public class Uye
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Telefon { get; set; }

        public ICollection<Randevu>? Randevular { get; set; }
        public ICollection<AiLog>? AiLoglar { get; set; }
    }
}
