namespace FitnessCenter.Web.Areas.Admin.Models
{
    /// <summary>
    /// Admin Dashboard için ViewModel
    /// Sistem geneli istatistikler ve salon bazlı finans verileri
    /// </summary>
    public class DashboardViewModel
    {
        // ===== Özet Kartlar =====
        
        /// <summary>
        /// Toplam salon sayısı
        /// </summary>
        public int ToplamSalon { get; set; }
        
        /// <summary>
        /// Toplam üye sayısı
        /// </summary>
        public int ToplamUye { get; set; }
        
        /// <summary>
        /// Toplam eğitmen sayısı
        /// </summary>
        public int ToplamEgitmen { get; set; }
        
        /// <summary>
        /// Toplam randevu sayısı
        /// </summary>
        public int ToplamRandevu { get; set; }

        // ===== Salon Bazlı Finans =====
        
        /// <summary>
        /// Her salonun finansal verileri
        /// </summary>
        public List<SalonFinansVm> SalonFinanslari { get; set; } = new();

        // ===== Zincir Toplamları =====
        
        /// <summary>
        /// Tüm salonların toplam geliri (TL)
        /// </summary>
        public decimal ToplamGelir { get; set; }
        
        /// <summary>
        /// Tüm salonların toplam gideri (TL)
        /// </summary>
        public decimal ToplamGider { get; set; }
        
        /// <summary>
        /// Zincir net kârı (TL)
        /// </summary>
        public decimal ToplamKar { get; set; }
    }

    /// <summary>
    /// Tek bir salonun finansal görünümü
    /// </summary>
    public class SalonFinansVm
    {
        public int SalonId { get; set; }
        public string SalonAdi { get; set; } = string.Empty;
        
        /// <summary>
        /// Salona kayıtlı aktif üye sayısı
        /// </summary>
        public int UyeSayisi { get; set; }
        
        /// <summary>
        /// Salonda çalışan aktif eğitmen sayısı
        /// </summary>
        public int EgitmenSayisi { get; set; }
        
        /// <summary>
        /// Yıllık üyelik geliri (UyeSayisi * 24.000 TL)
        /// </summary>
        public decimal Gelir { get; set; }
        
        /// <summary>
        /// Yıllık maaş gideri (Eğitmen maaşları * 12)
        /// </summary>
        public decimal Gider { get; set; }
        
        /// <summary>
        /// Net kâr (Gelir - Gider)
        /// </summary>
        public decimal Kar { get; set; }
    }
}
