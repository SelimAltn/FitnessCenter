using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            AppDbContext context,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.KullaniciAdi,
                Email = model.Email,
                ThemePreference = "Light" // Varsayılan tema
            };

            var result = await _userManager.CreateAsync(user, model.Sifre);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Member");
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        #endregion

        #region Login / Logout

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ApplicationUser user;

            if (model.KullaniciAdiVeyaEmail.Contains("@"))
                user = await _userManager.FindByEmailAsync(model.KullaniciAdiVeyaEmail);
            else
                user = await _userManager.FindByNameAsync(model.KullaniciAdiVeyaEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Kullanıcı adı veya e-posta bulunamadı.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Sifre, model.BeniHatirla, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Giriş başarısız. Bilgileri kontrol et.");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Settings (Tema)

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var model = new SettingsViewModel
            {
                ThemePreference = user.ThemePreference ?? "Light"
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // Tema tercihini kaydet
            user.ThemePreference = model.ThemePreference;
            await _userManager.UpdateAsync(user);

            TempData["SuccessMessage"] = "Ayarlarınız başarıyla kaydedildi.";
            return RedirectToAction("Settings");
        }

        #endregion

        #region Profile (Email/Şifre Değiştirme)

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                UserName = user.UserName
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // Email değişikliği doğrulama
            if (model.Email != user.Email)
            {
                // Benzersizlik kontrolü
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                    return View(model);
                }

                user.Email = model.Email;
                user.NormalizedEmail = model.Email.ToUpperInvariant();
            }

            // Şifre değişikliği (opsiyonel)
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                if (string.IsNullOrEmpty(model.CurrentPassword))
                {
                    ModelState.AddModelError("CurrentPassword", "Mevcut şifrenizi girmelisiniz.");
                    return View(model);
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
                if (!passwordCheck)
                {
                    ModelState.AddModelError("CurrentPassword", "Mevcut şifre yanlış.");
                    return View(model);
                }

                var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
                if (!changeResult.Succeeded)
                {
                    foreach (var error in changeResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }
            }

            await _userManager.UpdateAsync(user);

            // Uye tablosundaki email'i de güncelle (varsa)
            var uye = await _context.Uyeler.FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);
            if (uye != null && model.Email != uye.Email)
            {
                uye.Email = model.Email;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Profiliniz başarıyla güncellendi.";
            return RedirectToAction("Profile");
        }

        #endregion

        #region Delete Account

        [Authorize]
        [HttpGet]
        public IActionResult DeleteAccount()
        {
            return View(new DeleteAccountViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login");

            // Şifre doğrulama
            var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
            if (!passwordValid)
            {
                ModelState.AddModelError("Password", "Şifre yanlış.");
                return View(model);
            }

            // Transaction ile tüm verileri sil
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Uye'yi bul
                var uye = await _context.Uyeler
                    .Include(u => u.Randevular)
                    .Include(u => u.AiLoglar)
                    .Include(u => u.Uyelikler)
                    .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

                if (uye != null)
                {
                    // 2. AiLog'ları sil
                    if (uye.AiLoglar != null && uye.AiLoglar.Any())
                    {
                        _context.AiLoglar.RemoveRange(uye.AiLoglar);
                    }

                    // 3. Randevuları sil
                    if (uye.Randevular != null && uye.Randevular.Any())
                    {
                        _context.Randevular.RemoveRange(uye.Randevular);
                    }

                    // 4. Üyelikler cascade ile silinecek, ama elle de silebiliriz
                    if (uye.Uyelikler != null && uye.Uyelikler.Any())
                    {
                        _context.Uyelikler.RemoveRange(uye.Uyelikler);
                    }

                    // 5. Uye'yi sil
                    _context.Uyeler.Remove(uye);
                    await _context.SaveChangesAsync();
                }

                // 6. Support ticket'ları kullanıcı ID'si ile güncelle (anonim yap)
                var tickets = await _context.SupportTickets.Where(t => t.UserId == user.Id).ToListAsync();
                foreach (var ticket in tickets)
                {
                    ticket.UserId = null;
                }
                await _context.SaveChangesAsync();

                // 7. Identity kullanıcısını sil
                var deleteResult = await _userManager.DeleteAsync(user);
                if (!deleteResult.Succeeded)
                {
                    throw new Exception("Kullanıcı silinemedi: " + string.Join(", ", deleteResult.Errors.Select(e => e.Description)));
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Kullanıcı hesabı silindi: {UserId}, {UserName}", user.Id, user.UserName);

                // 8. Oturumu kapat
                await _signInManager.SignOutAsync();

                TempData["SuccessMessage"] = "Hesabınız başarıyla silindi.";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Hesap silme işlemi başarısız: {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Hesap silinirken bir hata oluştu. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        #endregion

        #region Access Denied

        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion
    }
}
