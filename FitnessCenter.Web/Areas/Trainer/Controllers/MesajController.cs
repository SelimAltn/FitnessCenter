using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Trainer.Controllers
{
    [Area("Trainer")]
    [Authorize(Policy = "TrainerOnly")]
    public class MesajController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMesajService _mesajService;
        private readonly IBildirimService _bildirimService;

        public MesajController(
            AppDbContext context, 
            UserManager<ApplicationUser> userManager,
            IMesajService mesajService,
            IBildirimService bildirimService)
        {
            _context = context;
            _userManager = userManager;
            _mesajService = mesajService;
            _bildirimService = bildirimService;
        }

        /// <summary>
        /// Tüm konuşmalar listesi
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var konusmalar = await _mesajService.GetKonusmalarAsync(user.Id);

            return View(konusmalar);
        }

        /// <summary>
        /// Belirli bir kullanıcı ile sohbet ekranı
        /// </summary>
        public async Task<IActionResult> Chat(string userId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Index");

            // Karşı tarafın bilgilerini al
            var karsiTaraf = await _userManager.FindByIdAsync(userId);
            if (karsiTaraf == null)
                return NotFound();

            // Admin mi kontrol et
            var isAdmin = await _userManager.IsInRoleAsync(karsiTaraf, "Admin");

            // Admin değilse, mesajlaşmaya izin var mı kontrol et
            if (!isAdmin)
            {
                var izinVar = await _mesajService.MesajlasmayaIzinVarMi(user.Id, userId);
                if (!izinVar)
                {
                    TempData["Error"] = "Bu kullanıcı ile mesajlaşabilmek için onaylı bir randevunuz olması gerekiyor.";
                    return RedirectToAction("Index");
                }
            }

            // Konuşmayı okundu işaretle
            await _mesajService.KonusmayiOkunduIsaretle(user.Id, userId);

            // Mesajları al
            var mesajlar = await _mesajService.GetKonusmaAsync(user.Id, userId);

            ViewData["KarsiTaraf"] = karsiTaraf;
            ViewData["KarsiTarafId"] = userId;
            ViewData["IsAdmin"] = isAdmin;

            return View(mesajlar);
        }

        /// <summary>
        /// Mesaj gönder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Gonder(string aliciId, string mesaj)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            if (string.IsNullOrWhiteSpace(mesaj))
            {
                TempData["Error"] = "Mesaj boş olamaz.";
                return RedirectToAction("Chat", new { userId = aliciId });
            }

            // Admin mi kontrol et
            var alici = await _userManager.FindByIdAsync(aliciId);
            if (alici == null)
                return NotFound();

            var isAdmin = await _userManager.IsInRoleAsync(alici, "Admin");
            var konusmaTipi = isAdmin ? "TrainerAdmin" : "TrainerUser";

            // Admin değilse izin kontrolü
            if (!isAdmin)
            {
                var izinVar = await _mesajService.MesajlasmayaIzinVarMi(user.Id, aliciId);
                if (!izinVar)
                {
                    TempData["Error"] = "Bu kullanıcı ile mesajlaşma izniniz yok.";
                    return RedirectToAction("Index");
                }
            }

            await _mesajService.GonderAsync(user.Id, aliciId, mesaj, konusmaTipi);

            return RedirectToAction("Chat", new { userId = aliciId });
        }

        /// <summary>
        /// Admin ile yeni sohbet başlat
        /// </summary>
        public async Task<IActionResult> AdminIletisim()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Bir admin bul
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            var admin = adminUsers.FirstOrDefault();

            if (admin == null)
            {
                TempData["Error"] = "Sistem yöneticisi bulunamadı.";
                return RedirectToAction("Index");
            }

            return RedirectToAction("Chat", new { userId = admin.Id });
        }
    }
}
