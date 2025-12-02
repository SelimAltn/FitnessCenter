namespace FitnessCenter.Web.Models.Entities
{
    public class EgitmenHizmet
    {
        public int EgitmenId { get; set; }
        public int HizmetId { get; set; }

        public Egitmen? Egitmen { get; set; }
        public Hizmet? Hizmet { get; set; }
    }
}
