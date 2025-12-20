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
    public class UyeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UyeController(AppDbContext context, UserManager<ApplicationUser> userManager)
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

            // Get all active memberships for this salon
            var uyelikler = await _context.Uyelikler
                .Include(u => u.Uye)
                .Where(u => u.SalonId == salon.Id && u.Durum == "Aktif")
                .OrderBy(u => u.Uye!.AdSoyad)
                .ToListAsync();

            ViewBag.Salon = salon;
            return View(uyelikler);
        }
    }
}
