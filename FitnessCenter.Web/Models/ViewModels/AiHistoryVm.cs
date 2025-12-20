namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI Öneri Geçmişi sayfası için ViewModel
    /// </summary>
    public class AiHistoryVm
    {
        public List<AiHistoryItemVm> Items { get; set; } = new();
        public string? TipFilter { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// Tek bir AI log kaydı için ViewModel
    /// </summary>
    public class AiHistoryItemVm
    {
        public int Id { get; set; }
        
        /// <summary>
        /// "Data" veya "Photo" mode
        /// </summary>
        public string Tip { get; set; } = "Data";
        
        /// <summary>
        /// Girdi özeti (SoruMetni)
        /// </summary>
        public string Girdi { get; set; } = string.Empty;
        
        /// <summary>
        /// Cevap özeti (CevapMetni)
        /// </summary>
        public string Cevap { get; set; } = string.Empty;
        
        /// <summary>
        /// Oluşturulma tarihi
        /// </summary>
        public DateTime Tarih { get; set; }
        
        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        public bool IsSuccess { get; set; }
        
        /// <summary>
        /// İşlem süresi (ms)
        /// </summary>
        public int? DurationMs { get; set; }
    }
}
