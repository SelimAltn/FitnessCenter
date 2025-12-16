using FitnessCenter.Web.Models.Entities;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// Admin Salon Dashboard için ViewModel
    /// </summary>
    public class SalonDetailsVm
    {
        // Salon bilgileri
        public Salon Salon { get; set; } = null!;

        // İstatistikler
        public int EgitmenSayisi { get; set; }
        public int UyeSayisi { get; set; }
        public int ToplamRandevuSayisi { get; set; }
        public int BugunRandevuSayisi { get; set; }
        public int BekleyenRandevuSayisi { get; set; }

        // Listeler
        public List<SalonUyeListItem> Uyeler { get; set; } = new();
        public List<SalonEgitmenListItem> Egitmenler { get; set; } = new();
        public List<Randevu> Randevular { get; set; } = new();

        // Filtreler
        public string? RandevuFiltre { get; set; } = "beklemede"; // bugün, hafta, beklemede, tumu
    }

    public class SalonUyeListItem
    {
        public int UyeId { get; set; }
        public int UyelikId { get; set; }
        public string AdSoyad { get; set; } = "";
        public string? Email { get; set; }
        public string? Telefon { get; set; }
        public string UyelikDurum { get; set; } = "";
        public DateTime BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
    }

    public class SalonEgitmenListItem
    {
        public int EgitmenId { get; set; }
        public string AdSoyad { get; set; } = "";
        public List<string> UzmanlikAlanlari { get; set; } = new();
        public string CalismaSaatleriOzet { get; set; } = "";
        public bool Aktif { get; set; }
    }
}
