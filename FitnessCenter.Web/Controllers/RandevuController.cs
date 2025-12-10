using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Randevular arasında bırakılacak minimum ara (dk)
        private const int MinAraDakika = 10;

        public RandevuController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Login olan kullanıcının Uye kaydını, ApplicationUserId üzerinden bul
        private async Task<Uye?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return null;

            var uye = await _context.Uyeler
                .Include(u => u.Uyelikler!)
                    .ThenInclude(u => u.Salon)
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            return uye;
        }

        // Dropdown’lar için helper (üyenin aktif olduğu şubeleri doldur)
        private async Task DoldurSelectListelerAsync(
            Uye uye,
            int? salonId = null,
            int? hizmetId = null,
            int? egitmenId = null)
        {
            // Üyenin aktif üyelikleri olan şube id’leri
            var aktifSalonIdler = await _context.Uyelikler
                .Where(x => x.UyeId == uye.Id && x.Durum == "Aktif")
                .Select(x => x.SalonId)
                .Distinct()
                .ToListAsync();

            var salonQuery = _context.Salonlar.AsQueryable();

            if (aktifSalonIdler.Any())
            {
                salonQuery = salonQuery.Where(s => aktifSalonIdler.Contains(s.Id));
            }
            else
            {
                // Hiç aktif salon yoksa, liste boş gelsin
                salonQuery = salonQuery.Where(s => false);
            }

            var salonlar = await salonQuery
                .OrderBy(s => s.Ad)
                .ToListAsync();

            var hizmetler = await _context.Hizmetler
                .OrderBy(h => h.Ad)
                .ToListAsync();

            var egitmenler = await _context.Egitmenler
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            ViewData["SalonId"] = new SelectList(salonlar, "Id", "Ad", salonId);
            ViewData["HizmetId"] = new SelectList(hizmetler, "Id", "Ad", hizmetId);
            ViewData["EgitmenId"] = new SelectList(egitmenler, "Id", "AdSoyad", egitmenId);
        }

        // GET: /Randevu
        // Üyenin kendi randevularını tarih / eğitmen / hizmet filtresi ile listele
        public async Task<IActionResult> Index(DateTime? tarih, int? egitmenId, int? hizmetId)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return View(Enumerable.Empty<Randevu>());
            }

            var query = _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .Where(r => r.UyeId == uye.Id);

            if (tarih.HasValue)
            {
                var d = tarih.Value.Date;
                query = query.Where(r => r.BaslangicZamani.Date == d);
            }

            if (egitmenId.HasValue)
                query = query.Where(r => r.EgitmenId == egitmenId.Value);

            if (hizmetId.HasValue)
                query = query.Where(r => r.HizmetId == hizmetId.Value);

            var liste = await query
                .OrderByDescending(r => r.BaslangicZamani)
                .ToListAsync();

            // Filtre dropdown’ları
            ViewData["EgitmenId"] = new SelectList(
                await _context.Egitmenler.OrderBy(e => e.AdSoyad).ToListAsync(),
                "Id", "AdSoyad", egitmenId
            );

            ViewData["HizmetId"] = new SelectList(
                await _context.Hizmetler.OrderBy(h => h.Ad).ToListAsync(),
                "Id", "Ad", hizmetId
            );

            ViewData["SeciliTarih"] = tarih?.ToString("yyyy-MM-dd");

            return View(liste);
        }

        // GET: /Randevu/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // Bu üyenin en az bir aktif şubesi var mı?
            bool aktifUyelikVar = await _context.Uyelikler.AnyAsync(u =>
                u.UyeId == uye.Id && u.Durum == "Aktif");

            if (!aktifUyelikVar)
            {
                TempData["Error"] = "Randevu oluşturabilmek için en az bir şubede aktif üyeliğiniz olması gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            await DoldurSelectListelerAsync(uye);

            // Varsayılan: bugün + 1 gün, saat 10:00 gibi
            var model = new Randevu
            {
                BaslangicZamani = DateTime.Today.AddDays(1).AddHours(10)
            };

            return View(model);
        }

        // POST: /Randevu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Randevu randevu)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı. Lütfen yetkili ile iletişime geçin.";
                return RedirectToAction(nameof(Index));
            }

            await DoldurSelectListelerAsync(uye, randevu.SalonId, randevu.HizmetId, randevu.EgitmenId);

            // Temel model doğrulaması
            if (!ModelState.IsValid)
            {
                return View(randevu);
            }

            // Hizmeti bul – süre hesaplamak için
            var hizmet = await _context.Hizmetler
                .FirstOrDefaultAsync(h => h.Id == randevu.HizmetId);

            if (hizmet == null)
            {
                ModelState.AddModelError(string.Empty, "Seçilen hizmet bulunamadı.");
                return View(randevu);
            }

            var baslangic = randevu.BaslangicZamani;
            var bitis = baslangic.AddMinutes(hizmet.SureDakika);

            randevu.BitisZamani = bitis;
            randevu.UyeId = uye.Id;            // güvenlik için
            randevu.Durum = "Beklemede";

            // ---------------- 1) Eğitmenin müsaitliği ----------------
            var gun = baslangic.DayOfWeek;
            var baslangicTime = baslangic.TimeOfDay;
            var bitisTime = bitis.TimeOfDay;

            bool musaitlikVar = await _context.Musaitlikler.AnyAsync(m =>
                m.EgitmenId == randevu.EgitmenId &&
                m.Gun == gun &&
                m.BaslangicSaati <= baslangicTime &&
                m.BitisSaati >= bitisTime
            );

            if (!musaitlikVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Seçilen eğitmen bu tarih ve saatte çalışmıyor. Lütfen eğitmenin müsait olduğu bir zaman seçin.");
            }

            // ---------------- 2) Aynı eğitmen için çakışan randevu var mı? ----------------
            var ayniGundekiRandevular = await _context.Randevular
                .Where(r =>
                    r.EgitmenId == randevu.EgitmenId &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool egitmenCakismaVar = ayniGundekiRandevular.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (egitmenCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında seçilen eğitmenin başka bir randevusu zaten var.");
            }

            // ---------------- 3) Aynı üyenin aynı saat aralığında başka randevusu var mı? ----------------
            var uyeRandevularAyniGun = await _context.Randevular
                .Where(r =>
                    r.UyeId == uye.Id &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool uyeCakismaVar = uyeRandevularAyniGun.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (uyeCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında size ait başka bir randevu zaten var. " +
                    "Aynı anda birden fazla eğitmenden randevu alınamaz.");
            }

            // ---------------- 4) Arka arkaya randevu yok kuralı (eğitmen için) ----------------
            bool araYok = ayniGundekiRandevular.Any(r =>
                Math.Abs((r.BaslangicZamani - bitis).TotalMinutes) < MinAraDakika ||
                Math.Abs((baslangic - r.BitisZamani).TotalMinutes) < MinAraDakika);

            if (araYok)
            {
                ModelState.AddModelError(string.Empty,
                    $"Randevular arasında en az {MinAraDakika} dakika ara olmalıdır.");
            }

            // Hata varsa formu geri göster
            if (!ModelState.IsValid)
            {
                return View(randevu);
            }

            _context.Randevular.Add(randevu);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu talebiniz oluşturuldu. Onaylandığında durum bilgisini bu ekrandan görebilirsiniz.";

            return RedirectToAction(nameof(Index));
        }

        // Üye kendi randevusunu iptal edebilsin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IptalEt(int id)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var randevu = await _context.Randevular
                .FirstOrDefaultAsync(r => r.Id == id && r.UyeId == uye.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (randevu.Durum == "İptal")
            {
                TempData["Info"] = "Bu randevu zaten iptal edilmiş.";
                return RedirectToAction(nameof(Index));
            }

            randevu.Durum = "İptal";
            await _context.SaveChangesAsync();

            TempData["Success"] = "Randevu başarıyla iptal edildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
