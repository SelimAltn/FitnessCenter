using FitnessCenter.Web.Data.Seed;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public DatabaseController(
            IServiceProvider serviceProvider, 
            ILogger<DatabaseController> logger,
            UserManager<ApplicationUser> userManager,
            AppDbContext context)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _userManager = userManager;
            _context = context;
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

        /// <summary>
        /// POST: /Admin/Database/CreateBranchManager
        /// Creates a test BranchManager user and assigns to first salon
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBranchManager()
        {
            try
            {
                // Check if BranchManager already exists
                var existingManager = await _userManager.FindByNameAsync("branchmanager");
                if (existingManager != null)
                {
                    // Assign to first salon without a manager
                    var salonWithoutManager = await _context.Salonlar
                        .FirstOrDefaultAsync(s => s.ManagerUserId == null);
                    
                    if (salonWithoutManager != null)
                    {
                        salonWithoutManager.ManagerUserId = existingManager.Id;
                        await _context.SaveChangesAsync();
                        TempData["Success"] = $"Mevcut BranchManager '{salonWithoutManager.Ad}' subesine atandi. Giris: branchmanager / 123456";
                    }
                    else
                    {
                        TempData["Success"] = "BranchManager zaten mevcut. Giris: branchmanager / 123456";
                    }
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                // Create BranchManager user
                var branchManagerUser = new ApplicationUser
                {
                    UserName = "branchmanager",
                    Email = "branchmanager@fitnesscenter.com",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(branchManagerUser, "123456");
                if (!createResult.Succeeded)
                {
                    TempData["Error"] = "Kullanici olusturulamadi: " + string.Join(", ", createResult.Errors.Select(e => e.Description));
                    return RedirectToAction("Index", "Home", new { area = "Admin" });
                }

                // Add to BranchManager role
                await _userManager.AddToRoleAsync(branchManagerUser, "BranchManager");

                // Assign to first salon
                var firstSalon = await _context.Salonlar.FirstOrDefaultAsync();
                if (firstSalon != null)
                {
                    firstSalon.ManagerUserId = branchManagerUser.Id;
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"BranchManager olusturuldu ve '{firstSalon.Ad}' subesine atandi. Giris: branchmanager / 123456";
                }
                else
                {
                    TempData["Success"] = "BranchManager olusturuldu (sube yok). Giris: branchmanager / 123456";
                }

                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBranchManager failed");
                TempData["Error"] = $"Hata: {ex.Message}";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }
        }
    }
}
