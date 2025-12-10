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
    public class MusaitlikController : Controller
    {
        private readonly AppDbContext _context;

        // Randevular arasında bırakacağımız min. ara (dk) burada da referans için dursun
        private const int MinAraDakika = 10;

        public MusaitlikController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Musaitlik
        public async Task<IActionResult> Index()
        {
            var liste = await _context.Musaitlikler
                .Include(m => m.Egitmen)
                .OrderBy(m => m.Egitmen.AdSoyad)
                .ThenBy(m => m.Gun)
                .ThenBy(m => m.BaslangicSaati)
                .ToListAsync();

            return View(liste);
        }

        // GET: Admin/Musaitlik/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var musaitlik = await _context.Musaitlikler
                .Include(m => m.Egitmen)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (musaitlik == null) return NotFound();

            return View(musaitlik);
        }

        // GET: Admin/Musaitlik/Create
        public async Task<IActionResult> Create()
        {
            await DoldurEgitmenSelectAsync();
            return View(new Musaitlik());
        }

        // POST: Admin/Musaitlik/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Musaitlik musaitlik)
        {
            await DoldurEgitmenSelectAsync(musaitlik.EgitmenId);

            TemelKontroller(musaitlik);

            // Aynı eğitmen + gün için çakışan blok var mı?
            CakismaKontrolu(musaitlik, isEdit: false);

            if (!ModelState.IsValid)
            {
                return View(musaitlik);
            }

            _context.Musaitlikler.Add(musaitlik);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Müsaitlik kaydı eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Musaitlik/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var musaitlik = await _context.Musaitlikler.FindAsync(id);
            if (musaitlik == null) return NotFound();

            await DoldurEgitmenSelectAsync(musaitlik.EgitmenId);
            return View(musaitlik);
        }

        // POST: Admin/Musaitlik/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Musaitlik musaitlik)
        {
            if (id != musaitlik.Id) return NotFound();

            await DoldurEgitmenSelectAsync(musaitlik.EgitmenId);

            TemelKontroller(musaitlik);
            CakismaKontrolu(musaitlik, isEdit: true);

            if (!ModelState.IsValid)
            {
                return View(musaitlik);
            }

            try
            {
                _context.Update(musaitlik);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Müsaitlik kaydı güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MusaitlikExists(musaitlik.Id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Musaitlik/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var musaitlik = await _context.Musaitlikler
                .Include(m => m.Egitmen)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (musaitlik == null) return NotFound();

            return View(musaitlik);
        }

        // POST: Admin/Musaitlik/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var musaitlik = await _context.Musaitlikler.FindAsync(id);
            if (musaitlik != null)
            {
                _context.Musaitlikler.Remove(musaitlik);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Müsaitlik kaydı silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ----------------- Helper metodlar -----------------

        private bool MusaitlikExists(int id)
        {
            return _context.Musaitlikler.Any(e => e.Id == id);
        }

        private async Task DoldurEgitmenSelectAsync(int? seciliEgitmenId = null)
        {
            var egitmenler = await _context.Egitmenler
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            ViewData["EgitmenId"] = new SelectList(egitmenler, "Id", "AdSoyad", seciliEgitmenId);
        }

        // Başlangıç < Bitiş gibi temel doğrulamalar
        private void TemelKontroller(Musaitlik m)
        {
            if (m.BaslangicSaati >= m.BitisSaati)
            {
                ModelState.AddModelError(string.Empty,
                    "Başlangıç saati bitiş saatinden küçük olmalıdır.");
            }
        }

        // Aynı eğitmen + gün için saat çakışması var mı?
        private void CakismaKontrolu(Musaitlik m, bool isEdit)
        {
            var query = _context.Musaitlikler
                .Where(x => x.EgitmenId == m.EgitmenId && x.Gun == m.Gun);

            if (isEdit)
            {
                // Kendini hariç tut
                query = query.Where(x => x.Id != m.Id);
            }

            var liste = query.ToList();

            bool cakismaVar = liste.Any(x =>
                !(m.BitisSaati <= x.BaslangicSaati || m.BaslangicSaati >= x.BitisSaati));

            if (cakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu eğitmen için bu gün/saat aralığında zaten bir çalışma bloğu tanımlı.");
            }

            // İstersen burada da arka arkaya bloklar için MinAraDakika kuralını ekleyebilirsin.
        }
    }
}
