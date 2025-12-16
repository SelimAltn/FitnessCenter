using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UzmanlikAlaniController : Controller
    {
        private readonly AppDbContext _context;

        public UzmanlikAlaniController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/UzmanlikAlani
        public async Task<IActionResult> Index()
        {
            var liste = await _context.UzmanlikAlanlari
                .OrderBy(u => u.Ad)
                .ToListAsync();
            return View(liste);
        }

        // GET: Admin/UzmanlikAlani/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/UzmanlikAlani/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UzmanlikAlani model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Aynı isimde var mı kontrol et
            if (await _context.UzmanlikAlanlari.AnyAsync(u => u.Ad == model.Ad))
            {
                ModelState.AddModelError("Ad", "Bu isimde bir uzmanlık alanı zaten mevcut.");
                return View(model);
            }

            _context.UzmanlikAlanlari.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{model.Ad}' uzmanlık alanı eklendi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/UzmanlikAlani/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var uzmanlik = await _context.UzmanlikAlanlari.FindAsync(id);
            if (uzmanlik == null) return NotFound();

            return View(uzmanlik);
        }

        // POST: Admin/UzmanlikAlani/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UzmanlikAlani model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Aynı isimde başka var mı kontrol et
            if (await _context.UzmanlikAlanlari.AnyAsync(u => u.Ad == model.Ad && u.Id != id))
            {
                ModelState.AddModelError("Ad", "Bu isimde bir uzmanlık alanı zaten mevcut.");
                return View(model);
            }

            try
            {
                _context.Update(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"'{model.Ad}' güncellendi.";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.UzmanlikAlanlari.AnyAsync(u => u.Id == id))
                    return NotFound();
                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/UzmanlikAlani/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var uzmanlik = await _context.UzmanlikAlanlari
                .Include(u => u.EgitmenUzmanliklari)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (uzmanlik == null) return NotFound();

            ViewData["KullananEgitmenSayisi"] = uzmanlik.EgitmenUzmanliklari?.Count ?? 0;

            return View(uzmanlik);
        }

        // POST: Admin/UzmanlikAlani/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uzmanlik = await _context.UzmanlikAlanlari
                .Include(u => u.EgitmenUzmanliklari)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (uzmanlik == null)
            {
                TempData["Error"] = "Uzmanlık alanı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Önce ilişkileri sil
            if (uzmanlik.EgitmenUzmanliklari?.Any() == true)
            {
                _context.EgitmenUzmanliklari.RemoveRange(uzmanlik.EgitmenUzmanliklari);
            }

            _context.UzmanlikAlanlari.Remove(uzmanlik);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{uzmanlik.Ad}' uzmanlık alanı silindi.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/UzmanlikAlani/ToggleAktif/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            var uzmanlik = await _context.UzmanlikAlanlari.FindAsync(id);
            if (uzmanlik == null)
            {
                TempData["Error"] = "Uzmanlık alanı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            uzmanlik.Aktif = !uzmanlik.Aktif;
            await _context.SaveChangesAsync();

            TempData["Success"] = uzmanlik.Aktif 
                ? $"'{uzmanlik.Ad}' aktif edildi." 
                : $"'{uzmanlik.Ad}' pasif edildi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
