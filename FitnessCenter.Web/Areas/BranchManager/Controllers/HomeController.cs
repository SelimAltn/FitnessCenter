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
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get the salon managed by current BranchManager user
        /// </summary>
        private async Task<Salon?> GetManagedSalonAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            // Find the SubeMuduru record for this user
            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.ApplicationUserId == user.Id && sm.Aktif);

            return mudur?.Salon;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

            // Find the SubeMuduru record for this user
            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.ApplicationUserId == user.Id && sm.Aktif);

            if (mudur == null)
            {
                TempData["Error"] = "Bu hesaba henuz bir sube atanmamis. Lutfen admin ile iletisime gecin.";
                return View("NoSalon");
            }

            var salon = mudur.Salon;
            if (salon == null) return View("NoSalon");

            // Load statistics for this salon
            var pendingAppointments = await _context.Randevular
                .Where(r => r.SalonId == salon.Id && r.Durum == "Beklemede")
                .CountAsync();

            var trainerCount = await _context.Egitmenler
                .Where(e => e.SalonId == salon.Id && e.Aktif)
                .CountAsync();

            var memberCount = await _context.Uyelikler
                .Where(u => u.SalonId == salon.Id && u.Durum == "Aktif")
                .CountAsync();

            ViewBag.Mudur = mudur;
            ViewBag.Salon = salon;
            ViewBag.PendingAppointments = pendingAppointments;
            ViewBag.TrainerCount = trainerCount;
            ViewBag.MemberCount = memberCount;

            return View();
        }
    }
}
