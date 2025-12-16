namespace FitnessCenter.Web.Models.Entities
{
    /// <summary>
    /// Eğitmen-Uzmanlık many-to-many ilişki tablosu
    /// </summary>
    public class EgitmenUzmanlik
    {
        public int EgitmenId { get; set; }
        public Egitmen Egitmen { get; set; } = null!;

        public int UzmanlikAlaniId { get; set; }
        public UzmanlikAlani UzmanlikAlani { get; set; } = null!;
    }
}
