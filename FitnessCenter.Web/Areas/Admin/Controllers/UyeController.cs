using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class UyeController : Controller
    {
        private readonly AppDbContext _context;

        public UyeController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Uye
        // Üyeleri ve üyeliklerini listele (gruplu görünüm için)
        public async Task<IActionResult> Index()
        {
            var uyeler = await _context.Uyeler
                .Include(u => u.Uyelikler!)
                    .ThenInclude(uy => uy.Salon)
                .Include(u => u.ApplicationUser)
                .OrderByDescending(u => u.Id)
                .ToListAsync();
            return View(uyeler);
        }

        // GET: Admin/Uye/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(x => x.Id == id);

            if (uye == null) return NotFound();

            return View(uye);
        }

        // GET: Admin/Uye/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Uye/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Uye uye)
        {
            if (!ModelState.IsValid)
            {
                return View(uye);
            }

            _context.Uyeler.Add(uye);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Uye/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var uye = await _context.Uyeler.FindAsync(id);
            if (uye == null) return NotFound();

            return View(uye);
        }

        // POST: Admin/Uye/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Uye uye)
        {
            if (id != uye.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(uye);
            }

            try
            {
                _context.Update(uye);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UyeExists(uye.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Uye/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(x => x.Id == id);

            if (uye == null) return NotFound();

            return View(uye);
        }

        // POST: Admin/Uye/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var uye = await _context.Uyeler.FindAsync(id);
            if (uye != null)
            {
                _context.Uyeler.Remove(uye);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool UyeExists(int id)
        {
            return _context.Uyeler.Any(e => e.Id == id);
        }
    }
}
    