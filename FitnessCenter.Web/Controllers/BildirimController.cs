using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Controllers
{
    /// <summary>
    /// Kullanıcı bildirimleri (Gelen Kutusu) Controller
    /// </summary>
    [Authorize]
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
        /// Gelen Kutusu - Tüm bildirimler
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var bildirimler = await _bildirimService.GetTumBildirimlerAsync(user.Id, 50);
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
                return RedirectToAction("Login", "Account");

            // Okundu işaretle
            await _bildirimService.OkunduIsaretle(id, user.Id);

            // Bildirim linkine yönlendir
            var bildirimler = await _bildirimService.GetTumBildirimlerAsync(user.Id);
            var bildirim = bildirimler.FirstOrDefault(b => b.Id == id);

            if (bildirim?.Link != null)
            {
                return Redirect(bildirim.Link);
            }

            return RedirectToAction("Index");
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
                return RedirectToAction("Login", "Account");

            await _bildirimService.TumunuOkunduIsaretle(user.Id);
            TempData["SuccessMessage"] = "Tüm bildirimler okundu olarak işaretlendi.";
            return RedirectToAction("Index");
        }

        /// <summary>
        /// Okunmamış bildirim sayısını JSON olarak döndür (AJAX için)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OkunmamisSayisi()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { sayi = 0 });

            var sayi = await _bildirimService.OkunmamisSayisiAsync(user.Id);
            return Json(new { sayi });
        }
    }
}
