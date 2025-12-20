using FitnessCenter.Web.Data.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin-only database management controller.
    /// Use with extreme caution!
    /// </summary>
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class DatabaseController : Controller
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DatabaseController> _logger;

        public DatabaseController(IServiceProvider serviceProvider, ILogger<DatabaseController> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// GET: /Admin/Database/Seed
        /// Shows confirmation page before running seed
        /// </summary>
        [HttpGet]
        public IActionResult Seed()
        {
            return View();
        }

        /// <summary>
        /// POST: /Admin/Database/RunSeed
        /// Runs the master seed (DESTRUCTIVE!)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RunSeed()
        {
            try
            {
                _logger.LogWarning("Master seed initiated by admin");
                
                await MasterSeedData.RunAsync(_serviceProvider);
                
                _logger.LogInformation("Master seed completed successfully");
                TempData["Success"] = "Veritabani seed islemi basariyla tamamlandi!";
                
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Master seed failed");
                TempData["Error"] = $"Seed hatasi: {ex.Message}";
                
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
        }
    }
}
