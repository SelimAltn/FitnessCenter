using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
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
        /// Tüm konuşmalar listesi (Admin için)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var konusmalar = await _mesajService.GetKonusmalarAsync(user.Id);

            // Tüm aktif eğitmenleri getir (yeni sohbet başlatmak için)
            var egitmenler = await _context.Egitmenler
                .Where(e => e.Aktif && e.ApplicationUserId != null)
                .OrderBy(e => e.AdSoyad)
                .Select(e => new { e.ApplicationUserId, e.AdSoyad })
                .ToListAsync();

            ViewData["Egitmenler"] = egitmenler;

            return View(konusmalar);
        }

        /// <summary>
        /// Belirli bir kullanıcı ile sohbet ekranı (Admin tarafı)
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

            // Konuşmayı okundu işaretle
            await _mesajService.KonusmayiOkunduIsaretle(user.Id, userId);

            // Mesajları al
            var mesajlar = await _mesajService.GetKonusmaAsync(user.Id, userId);

            // Karşı tarafın rolünü kontrol et
            var isTrainer = await _userManager.IsInRoleAsync(karsiTaraf, "Trainer");
            var isMember = await _userManager.IsInRoleAsync(karsiTaraf, "Member");

            ViewData["KarsiTaraf"] = karsiTaraf;
            ViewData["KarsiTarafId"] = userId;
            ViewData["IsTrainer"] = isTrainer;
            ViewData["IsMember"] = isMember;

            return View(mesajlar);
        }

        /// <summary>
        /// Mesaj gönder (Admin tarafı)
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

            var alici = await _userManager.FindByIdAsync(aliciId);
            if (alici == null)
                return NotFound();

            // Konuşma tipini belirle
            var isTrainer = await _userManager.IsInRoleAsync(alici, "Trainer");
            var konusmaTipi = isTrainer ? "TrainerAdmin" : "AdminMember";

            await _mesajService.GonderAsync(user.Id, aliciId, mesaj, konusmaTipi);

            // Alıcıya bildirim gönder
            await _bildirimService.OlusturAsync(
                userId: aliciId,
                baslik: "Yeni mesaj",
                mesaj: mesaj.Length > 50 ? mesaj.Substring(0, 50) + "..." : mesaj,
                tur: "NewMessage",
                iliskiliId: null,
                link: isTrainer ? $"/Trainer/Mesaj/Chat?userId={user.Id}" : $"/Mesaj/Chat?userId={user.Id}"
            );

            return RedirectToAction("Chat", new { userId = aliciId });
        }
    }
}
