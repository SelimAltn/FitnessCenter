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
        /// Eğitmen Dashboard - bugünkü ve yaklaşan randevular
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Eğitmeni bul
            var egitmen = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (egitmen == null)
            {
                TempData["Error"] = "Eğitmen profiliniz bulunamadı.";
                return View("Error");
            }

            // Bugünün randevuları
            var bugun = DateTime.Today;
            var bugunRandevular = await _context.Randevular
                .Include(r => r.Uye)
                .Include(r => r.Hizmet)
                .Include(r => r.Salon)
                .Where(r => r.EgitmenId == egitmen.Id && 
                           r.BaslangicZamani.Date == bugun &&
                           r.Durum == "Onaylandı")
                .OrderBy(r => r.BaslangicZamani)
                .ToListAsync();

            // Yaklaşan randevular (gelecek 7 gün)
            var gelecekHafta = bugun.AddDays(7);
            var yaklasanRandevular = await _context.Randevular
                .Include(r => r.Uye)
                .Include(r => r.Hizmet)
                .Include(r => r.Salon)
                .Where(r => r.EgitmenId == egitmen.Id && 
                           r.BaslangicZamani.Date > bugun &&
                           r.BaslangicZamani.Date <= gelecekHafta &&
                           r.Durum == "Onaylandı")
                .OrderBy(r => r.BaslangicZamani)
                .Take(10)
                .ToListAsync();

            // Bugünün çalışma saatleri
            var bugunGun = bugun.DayOfWeek;
            var bugunMusaitlik = egitmen.Musaitlikler?
                .Where(m => m.Gun == bugunGun)
                .OrderBy(m => m.BaslangicSaati)
                .ToList();

            ViewData["Egitmen"] = egitmen;
            ViewData["BugunRandevular"] = bugunRandevular;
            ViewData["YaklasanRandevular"] = yaklasanRandevular;
            ViewData["BugunMusaitlik"] = bugunMusaitlik;

            return View();
        }
    }
}
