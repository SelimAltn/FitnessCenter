using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class SalonsController : Controller
    {
        private readonly AppDbContext _context;

        public SalonsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Salons
        public async Task<IActionResult> Index()
        {
            var salons = await _context.Salonlar.ToListAsync();
            return View(salons);
        }

        // GET: Admin/Salons/Details/5 - SALON DASHBOARD
        public async Task<IActionResult> Details(int? id, string? filtre)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salonlar
                .FirstOrDefaultAsync(x => x.Id == id);

            if (salon == null) return NotFound();

            var bugun = DateTime.Today;
            var haftaSonu = bugun.AddDays(7);
            filtre ??= "beklemede";

            // İstatistikler
            var egitmenSayisi = await _context.Egitmenler.CountAsync(e => e.SalonId == id && e.Aktif);
            var uyeSayisi = await _context.Uyelikler.Where(u => u.SalonId == id).Select(u => u.UyeId).Distinct().CountAsync();
            var toplamRandevu = await _context.Randevular.CountAsync(r => r.SalonId == id);
            var bugunRandevu = await _context.Randevular.CountAsync(r => r.SalonId == id && r.BaslangicZamani.Date == bugun);
            var bekleyenRandevu = await _context.Randevular.CountAsync(r => r.SalonId == id && r.Durum == "Beklemede");

            // Üyeler listesi (bu salondaki üyeliklere sahip)
            var uyeler = await _context.Uyelikler
                .Where(u => u.SalonId == id)
                .Include(u => u.Uye)
                .OrderByDescending(u => u.BaslangicTarihi)
                .Select(u => new SalonUyeListItem
                {
                    UyeId = u.UyeId,
                    UyelikId = u.Id,
                    AdSoyad = u.Uye.AdSoyad,
                    Email = u.Uye.Email,
                    Telefon = u.Uye.Telefon,
                    UyelikDurum = u.Durum,
                    BaslangicTarihi = u.BaslangicTarihi,
                    BitisTarihi = u.BitisTarihi
                })
                .ToListAsync();

            // Eğitmenler listesi
            var egitmenler = await _context.Egitmenler
                .Where(e => e.SalonId == id)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .OrderBy(e => e.AdSoyad)
                .Select(e => new SalonEgitmenListItem
                {
                    EgitmenId = e.Id,
                    AdSoyad = e.AdSoyad,
                    UzmanlikAlanlari = e.EgitmenUzmanliklari!.Select(eu => eu.UzmanlikAlani!.Ad).ToList(),
                    CalismaSaatleriOzet = e.Musaitlikler != null && e.Musaitlikler.Any() 
                        ? $"{e.Musaitlikler.Count} gün/hafta" 
                        : "Belirlenmemiş",
                    Aktif = e.Aktif
                })
                .ToListAsync();

            // Randevular - filtreye göre
            IQueryable<Randevu> randevuQuery = _context.Randevular
                .Where(r => r.SalonId == id)
                .Include(r => r.Egitmen)
                .Include(r => r.Uye)
                .Include(r => r.Hizmet);

            randevuQuery = filtre switch
            {
                "bugun" => randevuQuery.Where(r => r.BaslangicZamani.Date == bugun),
                "hafta" => randevuQuery.Where(r => r.BaslangicZamani.Date >= bugun && r.BaslangicZamani.Date <= haftaSonu),
                "beklemede" => randevuQuery.Where(r => r.Durum == "Beklemede"),
                "tumu" => randevuQuery,
                _ => randevuQuery.Where(r => r.Durum == "Beklemede")
            };

            var randevular = await randevuQuery
                .OrderByDescending(r => r.BaslangicZamani)
                .Take(50) // Son 50 randevu
                .ToListAsync();

            var model = new SalonDetailsVm
            {
                Salon = salon,
                EgitmenSayisi = egitmenSayisi,
                UyeSayisi = uyeSayisi,
                ToplamRandevuSayisi = toplamRandevu,
                BugunRandevuSayisi = bugunRandevu,
                BekleyenRandevuSayisi = bekleyenRandevu,
                Uyeler = uyeler,
                Egitmenler = egitmenler,
                Randevular = randevular,
                RandevuFiltre = filtre
            };

            return View(model);
        }

        // GET: Admin/Salons/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Salons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Salon salon)
        {
            if (!ModelState.IsValid)
            {
                return View(salon);
            }

            _context.Salonlar.Add(salon);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Salons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salonlar.FindAsync(id);
            if (salon == null) return NotFound();

            return View(salon);
        }

        // POST: Admin/Salons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Salon salon)
        {
            if (id != salon.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(salon);
            }

            try
            {
                _context.Update(salon);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SalonExists(salon.Id))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Salons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var salon = await _context.Salonlar
                .FirstOrDefaultAsync(x => x.Id == id);

            if (salon == null) return NotFound();

            return View(salon);
        }

        // POST: Admin/Salons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var salon = await _context.Salonlar.FindAsync(id);
            if (salon != null)
            {
                _context.Salonlar.Remove(salon);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SalonExists(int id)
        {
            return _context.Salonlar.Any(e => e.Id == id);
        }
    }
}
