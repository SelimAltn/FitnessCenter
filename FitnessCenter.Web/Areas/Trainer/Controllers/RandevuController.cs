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
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RandevuController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Eğitmenin randevularını listele
        /// </summary>
        public async Task<IActionResult> Index(DateTime? tarih, string? durum)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Eğitmeni bul
            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (egitmen == null)
            {
                TempData["Error"] = "Eğitmen profiliniz bulunamadı.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Randevular
                .Include(r => r.Uye)
                .Include(r => r.Hizmet)
                .Include(r => r.Salon)
                .Where(r => r.EgitmenId == egitmen.Id)
                .AsQueryable();

            // Tarih filtresi
            if (tarih.HasValue)
            {
                query = query.Where(r => r.BaslangicZamani.Date == tarih.Value.Date);
            }

            // Durum filtresi
            if (!string.IsNullOrEmpty(durum))
            {
                query = query.Where(r => r.Durum == durum);
            }

            var randevular = await query
                .OrderByDescending(r => r.BaslangicZamani)
                .ToListAsync();

            // Filtre için dropdown değerleri
            ViewData["SeciliTarih"] = tarih?.ToString("yyyy-MM-dd");
            ViewData["SeciliDurum"] = durum;
            ViewData["Durumlar"] = new[] { "Beklemede", "Onaylandı", "İptal" };

            return View(randevular);
        }

        /// <summary>
        /// Randevu detayını görüntüle (read-only)
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var egitmen = await _context.Egitmenler
                .FirstOrDefaultAsync(e => e.ApplicationUserId == user.Id);

            if (egitmen == null)
                return NotFound();

            var randevu = await _context.Randevular
                .Include(r => r.Uye)
                .Include(r => r.Hizmet)
                .Include(r => r.Salon)
                .FirstOrDefaultAsync(r => r.Id == id && r.EgitmenId == egitmen.Id);

            if (randevu == null)
                return NotFound();

            return View(randevu);
        }
    }
}
