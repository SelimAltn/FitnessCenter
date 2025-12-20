using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class SubeMuduruController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SubeMuduruController> _logger;

        public SubeMuduruController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<SubeMuduruController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Admin/SubeMuduru
        public async Task<IActionResult> Index()
        {
            var mudurler = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .Include(sm => sm.ApplicationUser)
                .OrderBy(sm => sm.Salon!.Ad)
                .ToListAsync();

            return View(mudurler);
        }

        // GET: Admin/SubeMuduru/Create
        public async Task<IActionResult> Create()
        {
            // Get salons without a manager
            var salonsWithManager = await _context.SubeMudurler
                .Select(sm => sm.SalonId)
                .ToListAsync();

            var availableSalons = await _context.Salonlar
                .Where(s => !salonsWithManager.Contains(s.Id))
                .OrderBy(s => s.Ad)
                .ToListAsync();

            if (!availableSalons.Any())
            {
                TempData["Error"] = "Tum subelere mudur atanmis veya sube yok.";
                return RedirectToAction(nameof(Index));
            }

            ViewData["Salonlar"] = new SelectList(availableSalons, "Id", "Ad");
            return View(new SubeMuduru());
        }

        // POST: Admin/SubeMuduru/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubeMuduru model, string? Sifre)
        {
            // Remove validation for navigation properties
            ModelState.Remove("Salon");
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("ApplicationUserId");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check if salon already has a manager
                    var existingManager = await _context.SubeMudurler
                        .FirstOrDefaultAsync(sm => sm.SalonId == model.SalonId);

                    if (existingManager != null)
                    {
                        ModelState.AddModelError("SalonId", "Bu subeye zaten bir mudur atanmis.");
                        await LoadAvailableSalonsAsync();
                        return View(model);
                    }

                    // Create Identity user account
                    var username = GenerateUsername(model.AdSoyad);
                    var user = new ApplicationUser
                    {
                        UserName = username,
                        Email = model.Email,
                        EmailConfirmed = true
                    };

                    var password = Sifre ?? "123456";
                    var result = await _userManager.CreateAsync(user, password);

                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }
                        await LoadAvailableSalonsAsync();
                        return View(model);
                    }

                    // Add to BranchManager role
                    await _userManager.AddToRoleAsync(user, "BranchManager");

                    // Create SubeMuduru record
                    model.ApplicationUserId = user.Id;
                    model.OlusturulmaTarihi = DateTime.Now;

                    _context.SubeMudurler.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"Sube muduru '{model.AdSoyad}' basariyla olusturuldu. Giris: {username} / {password}";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SubeMuduru create error");
                    ModelState.AddModelError("", "Bir hata olustu: " + ex.Message);
                }
            }

            await LoadAvailableSalonsAsync();
            return View(model);
        }

        // GET: Admin/SubeMuduru/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.Id == id);

            if (mudur == null)
            {
                return NotFound();
            }

            // All salons for edit
            var allSalons = await _context.Salonlar.OrderBy(s => s.Ad).ToListAsync();
            ViewData["Salonlar"] = new SelectList(allSalons, "Id", "Ad", mudur.SalonId);

            return View(mudur);
        }

        // POST: Admin/SubeMuduru/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, SubeMuduru model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ModelState.Remove("Salon");
            ModelState.Remove("ApplicationUser");
            ModelState.Remove("ApplicationUserId");

            if (ModelState.IsValid)
            {
                try
                {
                    var existing = await _context.SubeMudurler.FindAsync(id);
                    if (existing == null)
                    {
                        return NotFound();
                    }

                    // Check if new salon already has another manager
                    if (existing.SalonId != model.SalonId)
                    {
                        var otherManager = await _context.SubeMudurler
                            .FirstOrDefaultAsync(sm => sm.SalonId == model.SalonId && sm.Id != id);

                        if (otherManager != null)
                        {
                            ModelState.AddModelError("SalonId", "Bu subeye baska bir mudur atanmis.");
                            var allSalons = await _context.Salonlar.OrderBy(s => s.Ad).ToListAsync();
                            ViewData["Salonlar"] = new SelectList(allSalons, "Id", "Ad", model.SalonId);
                            return View(model);
                        }
                    }

                    existing.AdSoyad = model.AdSoyad;
                    existing.Email = model.Email;
                    existing.Telefon = model.Telefon;
                    existing.SalonId = model.SalonId;
                    existing.Aktif = model.Aktif;

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Sube muduru guncellendi.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "SubeMuduru edit error");
                    ModelState.AddModelError("", "Bir hata olustu: " + ex.Message);
                }
            }

            var salons = await _context.Salonlar.OrderBy(s => s.Ad).ToListAsync();
            ViewData["Salonlar"] = new SelectList(salons, "Id", "Ad", model.SalonId);
            return View(model);
        }

        // GET: Admin/SubeMuduru/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.Id == id);

            if (mudur == null)
            {
                return NotFound();
            }

            return View(mudur);
        }

        // POST: Admin/SubeMuduru/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mudur = await _context.SubeMudurler.FindAsync(id);
            if (mudur != null)
            {
                // Also delete the identity user
                if (!string.IsNullOrEmpty(mudur.ApplicationUserId))
                {
                    var user = await _userManager.FindByIdAsync(mudur.ApplicationUserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                _context.SubeMudurler.Remove(mudur);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Sube muduru silindi.";
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadAvailableSalonsAsync()
        {
            var salonsWithManager = await _context.SubeMudurler
                .Select(sm => sm.SalonId)
                .ToListAsync();

            var availableSalons = await _context.Salonlar
                .Where(s => !salonsWithManager.Contains(s.Id))
                .OrderBy(s => s.Ad)
                .ToListAsync();

            ViewData["Salonlar"] = new SelectList(availableSalons, "Id", "Ad");
        }

        private string GenerateUsername(string adSoyad)
        {
            var parts = adSoyad.ToLower()
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var username = parts.Length >= 2
                ? $"sm.{parts[0]}{parts[1]}"
                : $"sm.{parts[0]}{DateTime.Now.Ticks % 1000}";

            return username;
        }
    }
}
