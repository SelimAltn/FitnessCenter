namespace FitnessCenter.Web.Models.Api
{
    public class TrainerDto
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; } = string.Empty;
        public string? Uzmanlik { get; set; }
        public string? SalonAdi { get; set; }
        public string? FotoUrl { get; set; }
    }
}
