using FitnessCenter.Web.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    [AllowAnonymous]
    public class SubelerimizController : Controller
    {
        private readonly AppDbContext _context;

        public SubelerimizController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var salonlar = await _context.Salonlar
                .OrderBy(s => s.Ad)
                .ToListAsync();

            // Get trainer counts for each salon
            var trainerCounts = await _context.Egitmenler
                .Where(e => e.Aktif && e.SalonId != null)
                .GroupBy(e => e.SalonId)
                .Select(g => new { SalonId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.SalonId!.Value, x => x.Count);

            // Get sample trainer names for each salon
            var trainerSamples = await _context.Egitmenler
                .Where(e => e.Aktif && e.SalonId != null)
                .GroupBy(e => e.SalonId)
                .Select(g => new
                {
                    SalonId = g.Key,
                    Names = g.OrderBy(e => e.AdSoyad).Take(3).Select(e => e.AdSoyad).ToList()
                })
                .ToDictionaryAsync(x => x.SalonId!.Value, x => x.Names);

            ViewBag.TrainerCounts = trainerCounts;
            ViewBag.TrainerSamples = trainerSamples;

            return View(salonlar);
        }
    }
}
