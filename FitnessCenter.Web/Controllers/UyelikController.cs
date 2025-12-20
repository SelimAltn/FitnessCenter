using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    [Authorize(Policy = "MemberOnly")]   // sadece giriş yapmış üyeler
    public class UyelikController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBildirimService _bildirimService;

        public UyelikController(
            AppDbContext context, 
            UserManager<ApplicationUser> userManager,
            IBildirimService bildirimService)
        {
            _context = context;
            _userManager = userManager;
            _bildirimService = bildirimService;
        }

        // Giriş yapan user için Uye kaydını bul (varsa)
        private async Task<Uye?> GetUyeForCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            return uye;
        }

        // GET: /Uyelik
        // Kullanıcının tüm şubelerdeki üyeliklerini listeler
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // login değilse logine at
            }

            var uye = await GetUyeForCurrentUserAsync();

            if (uye == null)
            {
                ViewBag.Mesaj = "Henüz herhangi bir şubede üyeliğiniz yok. 'Üye Ol' sayfasından başlatabilirsiniz.";
                return View(Enumerable.Empty<Uyelik>());
            }

            var uyelikler = await _context.Uyelikler
                .Include(u => u.Salon)
                .Where(u => u.UyeId == uye.Id)
                .OrderByDescending(u => u.BaslangicTarihi)
                .ToListAsync();

            return View(uyelikler);
        }

        // GET: /Uyelik/UyeOl
        public async Task<IActionResult> UyeOl()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            // Şube listesi
            var salonlar = await _context.Salonlar
                .OrderBy(s => s.Ad)
                .ToListAsync();

            ViewData["SalonId"] = new SelectList(salonlar, "Id", "Ad");

            // Eğer daha önce Uye oluşturulmuşsa, ad/telefonu dolduralım
            var mevcutUye = await GetUyeForCurrentUserAsync();

            var model = new UyelikOlViewModel
            {
                AdSoyad = mevcutUye?.AdSoyad ?? user.UserName ?? "",
                Telefon = mevcutUye?.Telefon
            };

            return View(model);
        }

        // POST: /Uyelik/UyeOl
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UyeOl(UyelikOlViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var salonlar = await _context.Salonlar
                .OrderBy(s => s.Ad)
                .ToListAsync();
            ViewData["SalonId"] = new SelectList(salonlar, "Id", "Ad", model.SalonId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1) Bu user için Uye var mı? (bir user = bir uye)
            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            if (uye == null)
            {
                // İlk kez üyelik alıyor → tek Uye burada oluşuyor
                uye = new Uye
                {
                    ApplicationUserId = user.Id,
                    AdSoyad = model.AdSoyad,
                    Email = user.Email ?? "",
                    Telefon = model.Telefon
                };

                _context.Uyeler.Add(uye);
                await _context.SaveChangesAsync();

                // ========== YENİ ÜYE OLUŞTURULDU - ADMİN'LERE BİLDİRİM ==========
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await _bildirimService.OlusturAsync(
                        userId: admin.Id,
                        baslik: "Yeni üye oluşturuldu",
                        mesaj: $"{model.AdSoyad} sisteme üye olarak eklendi.",
                        tur: "NewMember",
                        iliskiliId: uye.Id,
                        link: $"/Admin/Uye/Details/{uye.Id}"
                    );
                }
            }
            else
            {
                // Var olan üyenin temel bilgilerini güncelleyelim (opsiyonel)
                uye.AdSoyad = model.AdSoyad;
                uye.Telefon = model.Telefon;
                await _context.SaveChangesAsync();
            }

            // 2) Bu üyenin seçilen şubede zaten aktif üyeliği var mı?
            bool zatenUyelikVar = await _context.Uyelikler.AnyAsync(u =>
                u.UyeId == uye.Id &&
                u.SalonId == model.SalonId &&
                u.Durum == "Aktif");

            if (zatenUyelikVar)
            {
                ModelState.AddModelError(string.Empty, "Bu şubede zaten aktif bir üyeliğiniz bulunuyor.");
                return View(model);
            }

            // 3) Yeni uyelik kaydı oluştur
            var uyelik = new Uyelik
            {
                UyeId = uye.Id,
                SalonId = model.SalonId,
                BaslangicTarihi = DateTime.Today,
                Durum = "Aktif"
            };

            _context.Uyelikler.Add(uyelik);
            await _context.SaveChangesAsync();

            // Şube adını al
            var salon = await _context.Salonlar.FindAsync(model.SalonId);
            var salonAd = salon?.Ad ?? "Şube";

            // ========== YENİ ÜYELİK SATIN ALINDI - ADMİN'LERE BİLDİRİM ==========
            var adminUsersForMembership = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsersForMembership)
            {
                await _bildirimService.OlusturAsync(
                    userId: admin.Id,
                    baslik: "Yeni üyelik satın alındı",
                    mesaj: $"{model.AdSoyad} - {salonAd} şubesine üye oldu.",
                    tur: "NewMembership",
                    iliskiliId: uyelik.Id,
                    link: $"/Admin/Uye/Details/{uye.Id}"
                );
            }

            // ========== KULLANICIYA HOŞ GELDİN BİLDİRİMİ ==========
            await _bildirimService.OlusturAsync(
                userId: user.Id,
                baslik: "Aramıza hoş geldin! 🎉",
                mesaj: $"{salonAd} şubesine üyeliğiniz aktif. Sağlıklı yaşama hoş geldiniz!",
                tur: "MembershipWelcome",
                iliskiliId: uyelik.Id,
                link: "/Uyelik"
            );

            TempData["Success"] = "Üyeliğiniz oluşturuldu. Artık bu şubeden randevu alabilirsiniz.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Uyelik/IptalEt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IptalEt(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var uye = await GetUyeForCurrentUserAsync();
            if (uye == null)
            {
                TempData["Error"] = "Üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Sadece kendi üyeliğini iptal edebilir
            var uyelik = await _context.Uyelikler
                .Include(u => u.Salon)
                .FirstOrDefaultAsync(u => u.Id == id && u.UyeId == uye.Id);

            if (uyelik == null)
            {
                TempData["Error"] = "Üyelik bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (uyelik.Durum != "Aktif")
            {
                TempData["Error"] = "Sadece aktif üyelikler iptal edilebilir.";
                return RedirectToAction(nameof(Index));
            }

            // Üyeliği iptal et
            uyelik.Durum = "İptal";
            uyelik.BitisTarihi = DateTime.Today;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{uyelik.Salon?.Ad} şubesindeki üyeliğiniz iptal edildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
