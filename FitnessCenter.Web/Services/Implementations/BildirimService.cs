using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Services.Interfaces
{
    /// <summary>
    /// Bildirim servisi interface
    /// </summary>
    public interface IBildirimService
    {
        /// <summary>
        /// Kullanıcıya bildirim oluşturur
        /// </summary>
        Task OlusturAsync(string userId, string baslik, string mesaj, string tur, int? iliskiliId = null, string? link = null);

        /// <summary>
        /// Kullanıcının okunmamış bildirimlerini getirir
        /// </summary>
        Task<List<Bildirim>> GetOkunmamisAsync(string userId);

        /// <summary>
        /// Kullanıcının tüm bildirimlerini getirir
        /// </summary>
        Task<List<Bildirim>> GetTumBildirimlerAsync(string userId, int adet = 20);

        /// <summary>
        /// Bildirimi okundu olarak işaretle
        /// </summary>
        Task OkunduIsaretle(int bildirimId, string userId);

        /// <summary>
        /// Tüm bildirimleri okundu işaretle
        /// </summary>
        Task TumunuOkunduIsaretle(string userId);

        /// <summary>
        /// Okunmamış bildirim sayısı
        /// </summary>
        Task<int> OkunmamisSayisiAsync(string userId);
    }
}

namespace FitnessCenter.Web.Services.Implementations
{
    using FitnessCenter.Web.Services.Interfaces;
    
    public class BildirimService : IBildirimService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BildirimService> _logger;

        public BildirimService(AppDbContext context, ILogger<BildirimService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task OlusturAsync(string userId, string baslik, string mesaj, string tur, int? iliskiliId = null, string? link = null)
        {
            var bildirim = new Bildirim
            {
                UserId = userId,
                Baslik = baslik,
                Mesaj = mesaj,
                Tur = tur,
                IliskiliId = iliskiliId,
                Link = link,
                OlusturulmaTarihi = DateTime.UtcNow,
                Okundu = false
            };

            _context.Bildirimler.Add(bildirim);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bildirim oluşturuldu: {UserId}, {Tur}, {Baslik}", userId, tur, baslik);
        }

        public async Task<List<Bildirim>> GetOkunmamisAsync(string userId)
        {
            return await _context.Bildirimler
                .Where(b => b.UserId == userId && !b.Okundu)
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .ToListAsync();
        }

        public async Task<List<Bildirim>> GetTumBildirimlerAsync(string userId, int adet = 20)
        {
            return await _context.Bildirimler
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.OlusturulmaTarihi)
                .Take(adet)
                .ToListAsync();
        }

        public async Task OkunduIsaretle(int bildirimId, string userId)
        {
            var bildirim = await _context.Bildirimler
                .FirstOrDefaultAsync(b => b.Id == bildirimId && b.UserId == userId);

            if (bildirim != null)
            {
                bildirim.Okundu = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task TumunuOkunduIsaretle(string userId)
        {
            var bildirimler = await _context.Bildirimler
                .Where(b => b.UserId == userId && !b.Okundu)
                .ToListAsync();

            foreach (var b in bildirimler)
            {
                b.Okundu = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> OkunmamisSayisiAsync(string userId)
        {
            return await _context.Bildirimler
                .CountAsync(b => b.UserId == userId && !b.Okundu);
        }
    }
}
