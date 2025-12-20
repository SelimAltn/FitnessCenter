using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.BranchManager.Controllers
{
    [Area("BranchManager")]
    [Authorize(Roles = "BranchManager")]
    public class EgitmenController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EgitmenController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Salon?> GetManagedSalonAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.ApplicationUserId == user.Id && sm.Aktif);

            return mudur?.Salon;
        }

        public async Task<IActionResult> Index()
        {
            var salon = await GetManagedSalonAsync();
            if (salon == null)
            {
                TempData["Error"] = "Bu hesaba henuz bir sube atanmamis.";
                return RedirectToAction("Index", "Home");
            }

            var egitmenler = await _context.Egitmenler
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Where(e => e.SalonId == salon.Id)
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            ViewBag.Salon = salon;
            return View(egitmenler);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var salon = await GetManagedSalonAsync();
            if (salon == null) return Forbid();

            var egitmen = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.EgitmenHizmetler!)
                    .ThenInclude(eh => eh.Hizmet)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.Id == id && e.SalonId == salon.Id);

            if (egitmen == null)
            {
                TempData["Error"] = "Egitmen bulunamadi veya bu subeye ait degil.";
                return RedirectToAction(nameof(Index));
            }

            return View(egitmen);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            var salon = await GetManagedSalonAsync();
            if (salon == null) return Forbid();

            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(e => e.Id == id && e.SalonId == salon.Id);

            if (egitmen == null)
            {
                TempData["Error"] = "Egitmen bulunamadi veya bu subeye ait degil.";
                return RedirectToAction(nameof(Index));
            }

            egitmen.Aktif = !egitmen.Aktif;
            await _context.SaveChangesAsync();

            TempData["Success"] = egitmen.Aktif
                ? $"'{egitmen.AdSoyad}' aktif edildi."
                : $"'{egitmen.AdSoyad}' pasif edildi.";

            return RedirectToAction(nameof(Index));
        }
    }
}
