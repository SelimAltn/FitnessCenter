using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class EgitmenController : Controller
    {
        private readonly AppDbContext _context;

        public EgitmenController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Egitmen
        public async Task<IActionResult> Index()
        {
            var egitmenler = await _context.Egitmenler.ToListAsync();
            return View(egitmenler);
        }

        // GET: Admin/Egitmen/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(x => x.Id == id);

            if (egitmen == null) return NotFound();

            return View(egitmen);
        }

        // GET: Admin/Egitmen/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Egitmen/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Egitmen egitmen)
        {
            if (!ModelState.IsValid)
            {
                return View(egitmen);
            }

            _context.Egitmenler.Add(egitmen);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Egitmen/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler.FindAsync(id);
            if (egitmen == null) return NotFound();

            return View(egitmen);
        }

        // POST: Admin/Egitmen/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Egitmen egitmen)
        {
            if (id != egitmen.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(egitmen);
            }

            try
            {
                _context.Update(egitmen);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EgitmenExists(egitmen.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Egitmen/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(x => x.Id == id);

            if (egitmen == null) return NotFound();

            return View(egitmen);
        }

        // POST: Admin/Egitmen/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var egitmen = await _context.Egitmenler.FindAsync(id);
            if (egitmen != null)
            {
                _context.Egitmenler.Remove(egitmen);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EgitmenExists(int id)
        {
            return _context.Egitmenler.Any(e => e.Id == id);
        }
    }
}
