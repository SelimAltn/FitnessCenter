using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Trainer.Controllers
{
    [Area("Trainer")]
    [Authorize(Policy = "TrainerOnly")]
    public class ProfilController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfilController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Eğitmen profil bilgileri (read-only)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var egitmen = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (egitmen == null)
            {
                TempData["Error"] = "Eğitmen profiliniz bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            // Çalışma saatlerini gün bazlı grupla
            var calismaSaatleri = egitmen.Musaitlikler?
                .GroupBy(m => m.Gun)
                .OrderBy(g => (int)g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderBy(m => m.BaslangicSaati).ToList()
                );

            ViewData["CalismaSaatleri"] = calismaSaatleri;

            return View(egitmen);
        }
    }
}
