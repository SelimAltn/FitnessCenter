namespace FitnessCenter.Web.Models.Entities
{
    public class AiLog
    {
        public int Id { get; set; }

        public int? UyeId { get; set; }
        public string SoruMetni { get; set; } = null!;
        public string CevapMetni { get; set; } = null!;
        public DateTime OlusturulmaZamani { get; set; } = DateTime.UtcNow;

        public Uye? Uye { get; set; }
    }
}
