namespace FitnessCenter.Web.Models.Entities
{
    public class Musaitlik
    {
        public int Id { get; set; }

        public int EgitmenId { get; set; }
        public DayOfWeek Gun { get; set; }          // Pazartesi, Salı vs.
        public TimeSpan BaslangicSaati { get; set; }
        public TimeSpan BitisSaati { get; set; }

        public Egitmen? Egitmen { get; set; }
    }
}
