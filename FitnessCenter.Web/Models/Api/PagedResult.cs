namespace FitnessCenter.Web.Models.Api
{
    public class PagedResult<T>
    {
        // Klasik sayfalama zarfı: hem verileri hem de toplam sayıyı
        public IReadOnlyList<T> Items { get; set; } = new List<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }     
    }
}
