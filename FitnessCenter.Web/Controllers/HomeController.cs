using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            // Eğitmenler ana siteye erişemez, Trainer paneline yönlendir
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Trainer"))
            {
                return RedirectToAction("Index", "Home", new { area = "Trainer" });
            }
            
            // Şube müdürleri ana siteye erişemez, BranchManager paneline yönlendir
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("BranchManager"))
            {
                return RedirectToAction("Index", "Home", new { area = "BranchManager" });
            }

            // Member kullanıcıları Dashboard'a yönlendir
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Member") && !User.IsInRole("Admin"))
            {
                return RedirectToAction(nameof(Dashboard));
            }
            
            return View();
        }

        [Authorize(Policy = "MemberOnly")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var uye = await _context.Uyeler
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            // Dashboard verileri
            var aktifUyelikSayisi = 0;
            var yaklasanRandevuSayisi = 0;
            var sonAiOneriTarihi = (DateTime?)null;
            Randevu? yaklasanRandevu = null;

            if (uye != null)
            {
                // Tek sorguda aktif üyelik sayısı
                aktifUyelikSayisi = await _context.Uyelikler
                    .AsNoTracking()
                    .CountAsync(u => u.UyeId == uye.Id && u.Durum == "Aktif");

                var simdi = DateTime.Now;
                
                // Yaklaşan randevuları tek sorguda al (sayı + ilk randevu)
                var yaklasanRandevular = await _context.Randevular
                    .AsNoTracking()
                    .Include(r => r.Salon)
                    .Include(r => r.Hizmet)
                    .Include(r => r.Egitmen)
                    .Where(r => r.UyeId == uye.Id && r.BaslangicZamani > simdi && r.Durum != "İptal")
                    .OrderBy(r => r.BaslangicZamani)
                    .Take(10) // İlk 10 randevuyu al, sayıyı bundan çıkar
                    .ToListAsync();

                yaklasanRandevu = yaklasanRandevular.FirstOrDefault();
                yaklasanRandevuSayisi = yaklasanRandevular.Count;

                // Son AI önerisi - sadece tarih al, tüm kaydı değil
                sonAiOneriTarihi = await _context.AiLoglar
                    .AsNoTracking()
                    .Where(a => a.UyeId == uye.Id && a.IsSuccess)
                    .OrderByDescending(a => a.OlusturulmaZamani)
                    .Select(a => (DateTime?)a.OlusturulmaZamani)
                    .FirstOrDefaultAsync();
            }

            ViewBag.AktifUyelikSayisi = aktifUyelikSayisi;
            ViewBag.YaklasanRandevuSayisi = yaklasanRandevuSayisi;
            ViewBag.YaklasanRandevu = yaklasanRandevu;
            ViewBag.SonAiOneriTarihi = sonAiOneriTarihi;
            ViewBag.KullaniciAdi = user.UserName;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }
    }
}
