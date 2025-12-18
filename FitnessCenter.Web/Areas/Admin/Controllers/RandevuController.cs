using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBildirimService _bildirimService;

        public RandevuController(AppDbContext context, IBildirimService bildirimService)
        {
            _context = context;
            _bildirimService = bildirimService;
        }

        // GET: Admin/Randevu
        public async Task<IActionResult> Index(
            DateTime? tarih,
            int? salonId,
            int? egitmenId,
            int? uyeId,
            string? durum)
        {
            var query = _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .Include(r => r.Uye)
                .AsQueryable();

            if (tarih.HasValue)
            {
                var d = tarih.Value.Date;
                query = query.Where(r => r.BaslangicZamani.Date == d);
            }

            if (salonId.HasValue)
                query = query.Where(r => r.SalonId == salonId.Value);

            if (egitmenId.HasValue)
                query = query.Where(r => r.EgitmenId == egitmenId.Value);

            if (uyeId.HasValue)
                query = query.Where(r => r.UyeId == uyeId.Value);

            if (!string.IsNullOrWhiteSpace(durum))
                query = query.Where(r => r.Durum == durum);

            var liste = await query
                .OrderByDescending(r => r.BaslangicZamani)
                .ToListAsync();

            // Filtre dropdown’ları
            ViewData["SalonId"] = new SelectList(
                await _context.Salonlar.OrderBy(s => s.Ad).ToListAsync(),
                "Id", "Ad", salonId);

            ViewData["EgitmenId"] = new SelectList(
                await _context.Egitmenler.OrderBy(e => e.AdSoyad).ToListAsync(),
                "Id", "AdSoyad", egitmenId);

            ViewData["UyeId"] = new SelectList(
                await _context.Uyeler.OrderBy(u => u.AdSoyad).ToListAsync(),
                "Id", "AdSoyad", uyeId);

            var durumlar = new List<string> { "Beklemede", "Onaylandı", "İptal" };
            ViewData["Durum"] = new SelectList(durumlar, durum);

            ViewData["SeciliTarih"] = tarih?.ToString("yyyy-MM-dd");

            return View(liste);
        }

        // GET: Admin/Randevu/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var r = await _context.Randevular
                .Include(x => x.Salon)
                .Include(x => x.Hizmet)
                .Include(x => x.Egitmen)
                .Include(x => x.Uye)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null) return NotFound();

            return View(r);
        }

        // POST: Admin/Randevu/DurumDegistir
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id, string yeniDurum)
        {
            // Randevu ve ilişkili bilgileri al
            var r = await _context.Randevular
                .Include(x => x.Uye)
                    .ThenInclude(u => u!.ApplicationUser)
                .Include(x => x.Egitmen)
                .Include(x => x.Hizmet)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (r == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var izinliDurumlar = new[] { "Beklemede", "Onaylandı", "İptal" };
            if (!izinliDurumlar.Contains(yeniDurum))
            {
                TempData["Error"] = "Geçersiz durum isteği.";
                return RedirectToAction(nameof(Index));
            }

            // Zaten aynı durumdaysa boşuna kaydetmeyelim
            var eskiDurum = r.Durum;
            if (eskiDurum == yeniDurum)
            {
                TempData["Info"] = "Randevu zaten bu durumda.";
                return RedirectToAction(nameof(Index));
            }

            r.Durum = yeniDurum;
            await _context.SaveChangesAsync();

            // ========== KULLANICIYA BİLDİRİM GÖNDER ==========
            var kullaniciId = r.Uye?.ApplicationUserId;
            var egitmenAd = r.Egitmen?.AdSoyad ?? "Eğitmen";
            var hizmetAd = r.Hizmet?.Ad ?? "Hizmet";
            var randevuTarih = r.BaslangicZamani.ToString("dd.MM.yyyy HH:mm");
            var uyeAd = r.Uye?.AdSoyad ?? "Üye";

            if (!string.IsNullOrEmpty(kullaniciId))
            {
                if (yeniDurum == "Onaylandı")
                {
                    await _bildirimService.OlusturAsync(
                        userId: kullaniciId,
                        baslik: "Randevunuz onaylandı ✓",
                        mesaj: $"{randevuTarih} - {egitmenAd} - {hizmetAd}",
                        tur: "AppointmentApproved",
                        iliskiliId: r.Id,
                        link: "/Randevu"
                    );
                }
                else if (yeniDurum == "İptal")
                {
                    await _bildirimService.OlusturAsync(
                        userId: kullaniciId,
                        baslik: "Randevunuz iptal edildi",
                        mesaj: $"{randevuTarih} - {egitmenAd}",
                        tur: "AppointmentCancelledByAdmin",
                        iliskiliId: r.Id,
                        link: "/Randevu"
                    );
                }
            }

            // ========== EĞİTMENE BİLDİRİM GÖNDER ==========
            var egitmenUserId = r.Egitmen?.ApplicationUserId;
            if (!string.IsNullOrEmpty(egitmenUserId))
            {
                if (yeniDurum == "Onaylandı")
                {
                    await _bildirimService.OlusturAsync(
                        userId: egitmenUserId,
                        baslik: "Yeni onaylı randevunuz var",
                        mesaj: $"{randevuTarih} - {uyeAd} - {hizmetAd}",
                        tur: "TrainerAppointmentApproved",
                        iliskiliId: r.Id,
                        link: "/Trainer/Randevu"
                    );
                }
                else if (yeniDurum == "İptal")
                {
                    await _bildirimService.OlusturAsync(
                        userId: egitmenUserId,
                        baslik: "Randevu iptal edildi",
                        mesaj: $"{randevuTarih} - {uyeAd}",
                        tur: "TrainerAppointmentCancelled",
                        iliskiliId: r.Id,
                        link: "/Trainer/Randevu"
                    );
                }
            }

            TempData["Success"] = $"Randevu durumu '{yeniDurum}' olarak güncellendi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Randevu/TumunuOnayla
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TumunuOnayla()
        {
            // Beklemede olan tüm randevuları al
            var bekleyenRandevular = await _context.Randevular
                .Include(r => r.Uye)
                    .ThenInclude(u => u!.ApplicationUser)
                .Include(r => r.Egitmen)
                .Include(r => r.Hizmet)
                .Where(r => r.Durum == "Beklemede")
                .ToListAsync();

            if (!bekleyenRandevular.Any())
            {
                TempData["Info"] = "Beklemede olan randevu bulunmuyor.";
                return RedirectToAction(nameof(Index));
            }

            int onaylananSayi = 0;

            foreach (var randevu in bekleyenRandevular)
            {
                randevu.Durum = "Onaylandı";
                onaylananSayi++;

                var kullaniciId = randevu.Uye?.ApplicationUserId;
                var egitmenAd = randevu.Egitmen?.AdSoyad ?? "Eğitmen";
                var hizmetAd = randevu.Hizmet?.Ad ?? "Hizmet";
                var randevuTarih = randevu.BaslangicZamani.ToString("dd.MM.yyyy HH:mm");
                var uyeAd = randevu.Uye?.AdSoyad ?? "Üye";

                // Kullanıcıya bildirim
                if (!string.IsNullOrEmpty(kullaniciId))
                {
                    await _bildirimService.OlusturAsync(
                        userId: kullaniciId,
                        baslik: "Randevunuz onaylandı ✓",
                        mesaj: $"{randevuTarih} - {egitmenAd} - {hizmetAd}",
                        tur: "AppointmentApproved",
                        iliskiliId: randevu.Id,
                        link: "/Randevu"
                    );
                }

                // Eğitmene bildirim
                var egitmenUserId = randevu.Egitmen?.ApplicationUserId;
                if (!string.IsNullOrEmpty(egitmenUserId))
                {
                    await _bildirimService.OlusturAsync(
                        userId: egitmenUserId,
                        baslik: "Yeni onaylı randevunuz var",
                        mesaj: $"{randevuTarih} - {uyeAd} - {hizmetAd}",
                        tur: "TrainerAppointmentApproved",
                        iliskiliId: randevu.Id,
                        link: "/Trainer/Randevu"
                    );
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"{onaylananSayi} randevu başarıyla onaylandı ve kullanıcılara bildirim gönderildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
