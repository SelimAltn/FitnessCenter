using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
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

        public RandevuController(AppDbContext context)
        {
            _context = context;
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
            var r = await _context.Randevular.FindAsync(id);
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
            if (r.Durum == yeniDurum)
            {
                TempData["Info"] = "Randevu zaten bu durumda.";
                return RedirectToAction(nameof(Index));
            }

            r.Durum = yeniDurum;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Randevu durumu '{yeniDurum}' olarak güncellendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
