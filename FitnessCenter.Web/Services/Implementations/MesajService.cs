using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Services.Implementations
{
    public class MesajService : IMesajService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MesajService> _logger;
        private readonly IBildirimService _bildirimService;

        public MesajService(
            AppDbContext context, 
            ILogger<MesajService> logger,
            IBildirimService bildirimService)
        {
            _context = context;
            _logger = logger;
            _bildirimService = bildirimService;
        }

        public async Task<List<Mesaj>> GetKonusmaAsync(string kullanici1Id, string kullanici2Id, int adet = 50)
        {
            return await _context.Mesajlar
                .Where(m => 
                    (m.GonderenId == kullanici1Id && m.AliciId == kullanici2Id) ||
                    (m.GonderenId == kullanici2Id && m.AliciId == kullanici1Id))
                .OrderByDescending(m => m.GonderimTarihi)
                .Take(adet)
                .Include(m => m.Gonderen)
                .Include(m => m.Alici)
                .OrderBy(m => m.GonderimTarihi)
                .ToListAsync();
        }

        public async Task GonderAsync(string gonderenId, string aliciId, string icerik, string? konusmaTipi = null, int? randevuId = null)
        {
            var mesaj = new Mesaj
            {
                GonderenId = gonderenId,
                AliciId = aliciId,
                Icerik = icerik,
                GonderimTarihi = DateTime.UtcNow,
                Okundu = false,
                KonusmaTipi = konusmaTipi,
                RandevuId = randevuId
            };

            _context.Mesajlar.Add(mesaj);
            await _context.SaveChangesAsync();

            // Gönderen bilgisini al
            var gonderen = await _context.Users.FindAsync(gonderenId);
            var gonderenAd = gonderen?.UserName ?? "Bilinmeyen";

            // Alıcıya bildirim gönder
            await _bildirimService.OlusturAsync(
                userId: aliciId,
                baslik: "Yeni mesaj",
                mesaj: $"{gonderenAd}: {(icerik.Length > 50 ? icerik.Substring(0, 50) + "..." : icerik)}",
                tur: "YeniMesaj",
                iliskiliId: mesaj.Id,
                link: $"/Trainer/Mesaj/Chat?userId={gonderenId}"
            );

            _logger.LogInformation("Mesaj gönderildi: {GonderenId} -> {AliciId}", gonderenId, aliciId);
        }

        public async Task<int> OkunmamisSayisiAsync(string userId)
        {
            return await _context.Mesajlar
                .CountAsync(m => m.AliciId == userId && !m.Okundu);
        }

        public async Task OkunduIsaretle(int mesajId, string userId)
        {
            var mesaj = await _context.Mesajlar
                .FirstOrDefaultAsync(m => m.Id == mesajId && m.AliciId == userId);

            if (mesaj != null)
            {
                mesaj.Okundu = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task KonusmayiOkunduIsaretle(string aliciId, string gonderenId)
        {
            var mesajlar = await _context.Mesajlar
                .Where(m => m.AliciId == aliciId && m.GonderenId == gonderenId && !m.Okundu)
                .ToListAsync();

            foreach (var m in mesajlar)
            {
                m.Okundu = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> MesajlasmayaIzinVarMi(string trainerId, string userId)
        {
            // Trainer'ın Egitmen kaydını bul
            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(e => e.ApplicationUserId == trainerId);

            if (egitmen == null)
                return false;

            // User'ın Uye kaydını bul
            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(u => u.ApplicationUserId == userId);

            if (uye == null)
                return false;

            // Aralarında onaylı randevu var mı kontrol et
            return await _context.Randevular.AnyAsync(r =>
                r.EgitmenId == egitmen.Id &&
                r.UyeId == uye.Id &&
                r.Durum == "Onaylandı");
        }

        public async Task<List<KonusmaOzetVm>> GetKonusmalarAsync(string userId)
        {
            // Kullanıcının dahil olduğu tüm mesajları al
            var mesajlar = await _context.Mesajlar
                .Where(m => m.GonderenId == userId || m.AliciId == userId)
                .Include(m => m.Gonderen)
                .Include(m => m.Alici)
                .OrderByDescending(m => m.GonderimTarihi)
                .ToListAsync();

            // Konuşmaları grupla
            var konusmalar = mesajlar
                .GroupBy(m => m.GonderenId == userId ? m.AliciId : m.GonderenId)
                .Select(g => new KonusmaOzetVm
                {
                    KarsiTarafId = g.Key,
                    KarsiTarafAdi = g.First().GonderenId == userId 
                        ? g.First().Alici?.UserName ?? "Bilinmeyen"
                        : g.First().Gonderen?.UserName ?? "Bilinmeyen",
                    SonMesaj = g.First().Icerik,
                    SonMesajTarihi = g.First().GonderimTarihi,
                    OkunmamisSayisi = g.Count(m => m.AliciId == userId && !m.Okundu),
                    KonusmaTipi = g.First().KonusmaTipi
                })
                .OrderByDescending(k => k.SonMesajTarihi)
                .ToList();

            return konusmalar;
        }
    }
}
