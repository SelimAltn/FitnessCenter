using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class MusaitlikController : Controller
    {
        private readonly AppDbContext _context;

        public MusaitlikController(AppDbContext context)
        {
            _context = context;
        }

        // Müsaitlikleri Listeleme
        public async Task<IActionResult> Index()
        {
            // Include(m => m.Egitmen) dedik ki tabloda hocanın adını görebilelim.
            var musaitlikler = await _context.Musaitlikler
                .Include(m => m.Egitmen)
                .OrderBy(m => m.Gun) // Günlere göre sıralı gelsin
                .ThenBy(m => m.BaslangicSaati)
                .ToListAsync();

            return View(musaitlikler);
        }

        // Yeni Müsaitlik Ekleme Sayfası
        public IActionResult Create()
        {
            // Dropdown (Açılır kutu) için hocaları View'a gönderiyoruz
            ViewData["EgitmenId"] = new SelectList(_context.Egitmenler, "Id", "AdSoyad");
            return View();
        }

        // Yeni Müsaitlik Kaydetme (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Musaitlik musaitlik)
        {
            if (ModelState.IsValid)
            {
                _context.Add(musaitlik);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Hata olursa dropdown'ı tekrar doldurup sayfayı geri göster
            ViewData["EgitmenId"] = new SelectList(_context.Egitmenler, "Id", "AdSoyad", musaitlik.EgitmenId);
            return View(musaitlik);
        }

        // Silme İşlemi (Hatalı giriş olursa silmek için)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var musaitlik = await _context.Musaitlikler.Include(m => m.Egitmen).FirstOrDefaultAsync(m => m.Id == id);
            if (musaitlik == null) return NotFound();

            return View(musaitlik);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var musaitlik = await _context.Musaitlikler.FindAsync(id);
            if (musaitlik != null)
            {
                _context.Musaitlikler.Remove(musaitlik);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}