using FitnessCenter.Web.Models.Entities;

namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// Mesajlaşma servisi interface
    /// Trainer ↔ User ve Trainer ↔ Admin mesajlaşma işlemlerini yönetir
    /// </summary>
    public interface IMesajService
    {
        /// <summary>
        /// İki kullanıcı arasındaki konuşmayı getirir
        /// </summary>
        Task<List<Mesaj>> GetKonusmaAsync(string kullanici1Id, string kullanici2Id, int adet = 50);

        /// <summary>
        /// Mesaj gönderir
        /// </summary>
        Task GonderAsync(string gonderenId, string aliciId, string icerik, string? konusmaTipi = null, int? randevuId = null);

        /// <summary>
        /// Kullanıcının okunmamış mesaj sayısını getirir
        /// </summary>
        Task<int> OkunmamisSayisiAsync(string userId);

        /// <summary>
        /// Mesajı okundu olarak işaretler
        /// </summary>
        Task OkunduIsaretle(int mesajId, string userId);

        /// <summary>
        /// Bir konuşmadaki tüm mesajları okundu işaretle
        /// </summary>
        Task KonusmayiOkunduIsaretle(string aliciId, string gonderenId);

        /// <summary>
        /// Trainer ile User arasında mesajlaşmaya izin var mı kontrol eder
        /// (aralarında onaylı randevu olmalı)
        /// </summary>
        Task<bool> MesajlasmayaIzinVarMi(string trainerId, string userId);

        /// <summary>
        /// Kullanıcının tüm konuşmalarını getirir (son mesaj ile birlikte)
        /// </summary>
        Task<List<KonusmaOzetVm>> GetKonusmalarAsync(string userId);
    }

    /// <summary>
    /// Konuşma özeti ViewModel
    /// </summary>
    public class KonusmaOzetVm
    {
        public string KarsiTarafId { get; set; } = null!;
        public string KarsiTarafAdi { get; set; } = null!;
        public string? SonMesaj { get; set; }
        public DateTime? SonMesajTarihi { get; set; }
        public int OkunmamisSayisi { get; set; }
        public string? KonusmaTipi { get; set; }
    }
}
