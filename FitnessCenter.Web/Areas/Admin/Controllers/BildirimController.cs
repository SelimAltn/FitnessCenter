using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin bildirim yönetimi Controller
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BildirimController : Controller
    {
        private readonly IBildirimService _bildirimService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BildirimController(
            IBildirimService bildirimService,
            UserManager<ApplicationUser> userManager)
        {
            _bildirimService = bildirimService;
            _userManager = userManager;
        }

        /// <summary>
        /// Admin bildirim listesi
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            var bildirimler = await _bildirimService.GetTumBildirimlerAsync(user.Id, 100);
            return View(bildirimler);
        }

        /// <summary>
        /// Bildirimi okundu işaretle ve ilgili sayfaya yönlendir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Oku(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            // Okundu işaretle
            await _bildirimService.OkunduIsaretle(id, user.Id);

            // Bildirim linkine yönlendir
            var bildirimler = await _bildirimService.GetTumBildirimlerAsync(user.Id);
            var bildirim = bildirimler.FirstOrDefault(b => b.Id == id);

            if (bildirim?.Link != null)
            {
                return Redirect(bildirim.Link);
            }

            // Link yoksa admin randevu listesine dön
            return RedirectToAction("Index", "Randevu", new { area = "Admin" });
        }

        /// <summary>
        /// Tüm bildirimleri okundu işaretle
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TumunuOku()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account", new { area = "" });

            await _bildirimService.TumunuOkunduIsaretle(user.Id);
            TempData["Success"] = "Tüm bildirimler okundu olarak işaretlendi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
