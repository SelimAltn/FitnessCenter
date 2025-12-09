using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    //[Authorize(Policy = "AdminOnly")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;

        public RandevuController(AppDbContext context)
        {
            _context = context;
        }

        // 1. LİSTELEME
        public async Task<IActionResult> Index()
        {
            // Randevuları getirirken ilişkili tabloları (Salon, Hizmet, Egitmen, Uye) dahil ediyoruz
            var randevular = await _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .Include(r => r.Uye)
                .OrderByDescending(r => r.BaslangicZamani) // En yeni randevu en üstte
                .ToListAsync();

            return View(randevular);
        }

        // 2. YENİ RANDEVU SAYFASI (GET)
        public IActionResult Create()
        {
            // Dropdownlar için verileri hazırlayıp View'a (Bag) atıyoruz
            ViewData["SalonId"] = new SelectList(_context.Salonlar, "Id", "Ad");
            ViewData["HizmetId"] = new SelectList(_context.Hizmetler, "Id", "Ad");
            ViewData["EgitmenId"] = new SelectList(_context.Egitmenler, "Id", "AdSoyad");
            ViewData["UyeId"] = new SelectList(_context.Uyeler, "Id", "AdSoyad");

            return View();
        }

        // 3. RANDEVU KAYDETME (POST) - Şimdilik boş, bir sonraki adımda mantığı kuracağız
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Randevu randevu)
        {
            // Buraya "Hoca bu saatte müsait mi?" ve "Başka randevusu var mı?" kontrolünü yazacağız.
            // Şimdilik basit kayıt yapalım test için.

            if (ModelState.IsValid)
            {
                // Randevu bitiş saatini otomatik hesaplayalım (Hizmet süresine göre)
                var hizmet = await _context.Hizmetler.FindAsync(randevu.HizmetId);
                if (hizmet != null)
                {
                    randevu.BitisZamani = randevu.BaslangicZamani.AddMinutes(hizmet.SureDakika);
                }

                _context.Add(randevu);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata varsa listeleri tekrar doldur
            ViewData["SalonId"] = new SelectList(_context.Salonlar, "Id", "Ad", randevu.SalonId);
            ViewData["HizmetId"] = new SelectList(_context.Hizmetler, "Id", "Ad", randevu.HizmetId);
            ViewData["EgitmenId"] = new SelectList(_context.Egitmenler, "Id", "AdSoyad", randevu.EgitmenId);
            ViewData["UyeId"] = new SelectList(_context.Uyeler, "Id", "AdSoyad", randevu.UyeId);

            return View(randevu);
        }
    }
}