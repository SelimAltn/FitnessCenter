using FitnessCenter.Web.Areas.Admin.Models;
using FitnessCenter.Web.Data.Context;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        
        /// <summary>
        /// Yıllık üyelik ücreti (TL)
        /// </summary>
        private const decimal YillikUyelikUcreti = 24000m;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Admin Panel Landing - Sadece Zincir Özet + Hızlı Erişim
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Sadece özet metrikleri hesapla
            var salonUyeCounts = await _context.Uyelikler
                .Where(u => u.Durum == "Aktif")
                .GroupBy(u => u.SalonId)
                .Select(g => new { SalonId = g.Key, Count = g.Count() })
                .ToListAsync();

            var salonEgitmenData = await _context.Egitmenler
                .Where(e => e.SalonId != null && e.Aktif)
                .GroupBy(e => e.SalonId)
                .Select(g => new
                {
                    SalonId = g.Key,
                    ToplamMaas = g.Sum(e => e.Maas ?? 0)
                })
                .ToListAsync();

            var toplamGelir = salonUyeCounts.Sum(x => x.Count) * YillikUyelikUcreti;
            var toplamGider = salonEgitmenData.Sum(x => x.ToplamMaas) * 12;

            var model = new DashboardViewModel
            {
                ToplamGelir = toplamGelir,
                ToplamGider = toplamGider,
                ToplamKar = toplamGelir - toplamGider
            };

            return View(model);
        }

        /// <summary>
        /// Dashboard - Tam istatistikler (KPI + Finans Tablosu + Özet)
        /// </summary>
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                // ===== Özet Sayılar =====
                ToplamSalon = await _context.Salonlar.CountAsync(),
                ToplamUye = await _context.Uyeler.CountAsync(),
                ToplamEgitmen = await _context.Egitmenler.CountAsync(),
                ToplamRandevu = await _context.Randevular.CountAsync()
            };

            // ===== Salon Bazlı Üye Sayısı (Uyelik tablosundan) =====
            var salonUyeCounts = await _context.Uyelikler
                .Where(u => u.Durum == "Aktif")
                .GroupBy(u => u.SalonId)
                .Select(g => new { SalonId = g.Key, Count = g.Count() })
                .ToListAsync();

            // ===== Salon Bazlı Eğitmen Maaş Toplamı =====
            var salonEgitmenData = await _context.Egitmenler
                .Where(e => e.SalonId != null && e.Aktif)
                .GroupBy(e => e.SalonId)
                .Select(g => new
                {
                    SalonId = g.Key,
                    ToplamMaas = g.Sum(e => e.Maas ?? 0),
                    Count = g.Count()
                })
                .ToListAsync();

            // ===== Tüm Salonları Al ve Finansları Hesapla =====
            var salonlar = await _context.Salonlar
                .OrderBy(s => s.Ad)
                .ToListAsync();

            model.SalonFinanslari = salonlar.Select(s =>
            {
                var uyeCount = salonUyeCounts.FirstOrDefault(x => x.SalonId == s.Id)?.Count ?? 0;
                var egitmenData = salonEgitmenData.FirstOrDefault(x => x.SalonId == s.Id);

                var gelir = uyeCount * YillikUyelikUcreti;
                var gider = (egitmenData?.ToplamMaas ?? 0) * 12; // Aylık maaş → Yıllık

                return new SalonFinansVm
                {
                    SalonId = s.Id,
                    SalonAdi = s.Ad,
                    UyeSayisi = uyeCount,
                    EgitmenSayisi = egitmenData?.Count ?? 0,
                    Gelir = gelir,
                    Gider = gider,
                    Kar = gelir - gider
                };
            }).ToList();

            // ===== Zincir Toplamları =====
            model.ToplamGelir = model.SalonFinanslari.Sum(x => x.Gelir);
            model.ToplamGider = model.SalonFinanslari.Sum(x => x.Gider);
            model.ToplamKar = model.ToplamGelir - model.ToplamGider;

            return View(model);
        }
    }
}

