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

        public MusaitlikController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Musaitlik
        public async Task<IActionResult> Index()
        {
            var musaitlikler = await _context.Musaitlikler
                .Include(m => m.Egitmen)
                .OrderBy(m => m.Egitmen!.AdSoyad)
                .ThenBy(m => m.Gun)
                .ThenBy(m => m.BaslangicSaati)
                .ToListAsync();

            return View(musaitlikler);
        }

        // GET: Admin/Musaitlik/Create
        public async Task<IActionResult> Create()
        {
            await DoldurEgitmenDropDownAsync();
            return View();
        }

        // POST: Admin/Musaitlik/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Musaitlik musaitlik)
        {
            await DoldurEgitmenDropDownAsync(musaitlik.EgitmenId);

            // Basit mantık kontrolleri
            if (musaitlik.BitisSaati <= musaitlik.BaslangicSaati)
            {
                ModelState.AddModelError(string.Empty, "Bitiş saati, başlangıç saatinden sonra olmalıdır.");
            }

            // Aynı eğitmen + aynı gün için çakışan blok var mı?
            bool cakisanKayitVar = await _context.Musaitlikler.AnyAsync(m =>
                m.EgitmenId == musaitlik.EgitmenId &&
                m.Gun == musaitlik.Gun &&
                // saat aralığı örtüşüyor mu?
                !(musaitlik.BitisSaati <= m.BaslangicSaati || musaitlik.BaslangicSaati >= m.BitisSaati));

            if (cakisanKayitVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığı, seçilen eğitmenin mevcut başka bir müsaitlik kaydıyla çakışıyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(musaitlik);
            }

            _context.Musaitlikler.Add(musaitlik);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Musaitlik/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var musaitlik = await _context.Musaitlikler.FindAsync(id);
            if (musaitlik == null) return NotFound();

            await DoldurEgitmenDropDownAsync(musaitlik.EgitmenId);
            return View(musaitlik);
        }

        // POST: Admin/Musaitlik/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Musaitlik musaitlik)
        {
            if (id != musaitlik.Id) return NotFound();

            await DoldurEgitmenDropDownAsync(musaitlik.EgitmenId);

            if (musaitlik.BitisSaati <= musaitlik.BaslangicSaati)
            {
                ModelState.AddModelError(string.Empty, "Bitiş saati, başlangıç saatinden sonra olmalıdır.");
            }

            bool cakisanKayitVar = await _context.Musaitlikler.AnyAsync(m =>
                m.Id != musaitlik.Id &&
                m.EgitmenId == musaitlik.EgitmenId &&
                m.Gun == musaitlik.Gun &&
                !(musaitlik.BitisSaati <= m.BaslangicSaati || musaitlik.BaslangicSaati >= m.BitisSaati));

            if (cakisanKayitVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığı, seçilen eğitmenin mevcut başka bir müsaitlik kaydıyla çakışıyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(musaitlik);
            }

            try
            {
                _context.Update(musaitlik);
                await _context.SaveChangesAsync();
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
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task DoldurEgitmenDropDownAsync(int? seciliEgitmenId = null)
        {
            var egitmenler = await _context.Egitmenler
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            ViewData["EgitmenId"] = new SelectList(egitmenler, "Id", "AdSoyad", seciliEgitmenId);
        }

        private bool MusaitlikExists(int id)
        {
            return _context.Musaitlikler.Any(e => e.Id == id);
        }
    }
}
