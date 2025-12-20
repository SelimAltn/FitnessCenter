using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.BranchManager.Controllers
{
    [Area("BranchManager")]
    [Authorize(Roles = "BranchManager")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBildirimService _bildirimService;

        public RandevuController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IBildirimService bildirimService)
        {
            _context = context;
            _userManager = userManager;
            _bildirimService = bildirimService;
        }

        private async Task<Salon?> GetManagedSalonAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            var mudur = await _context.SubeMudurler
                .Include(sm => sm.Salon)
                .FirstOrDefaultAsync(sm => sm.ApplicationUserId == user.Id && sm.Aktif);

            return mudur?.Salon;
        }

        public async Task<IActionResult> Index(string? durum = null)
        {
            var salon = await GetManagedSalonAsync();
            if (salon == null)
            {
                TempData["Error"] = "Bu hesaba henuz bir sube atanmamis.";
                return RedirectToAction("Index", "Home");
            }

            var query = _context.Randevular
                .Include(r => r.Uye)
                .Include(r => r.Egitmen)
                .Include(r => r.Hizmet)
                .Where(r => r.SalonId == salon.Id);

            if (!string.IsNullOrEmpty(durum))
            {
                query = query.Where(r => r.Durum == durum);
            }

            var randevular = await query
                .OrderByDescending(r => r.BaslangicZamani)
                .ToListAsync();

            ViewBag.Salon = salon;
            ViewBag.SelectedDurum = durum;
            return View(randevular);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(int id)
        {
            var salon = await GetManagedSalonAsync();
            if (salon == null) return Forbid();

            var randevu = await _context.Randevular
                .Include(r => r.Uye)
                .ThenInclude(u => u!.ApplicationUser)
                .FirstOrDefaultAsync(r => r.Id == id && r.SalonId == salon.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadi veya bu subeye ait degil.";
                return RedirectToAction(nameof(Index));
            }

            randevu.Durum = "Onaylandi";
            await _context.SaveChangesAsync();

            // Notify the member
            if (randevu.Uye?.ApplicationUserId != null)
            {
                await _bildirimService.OlusturAsync(
                    userId: randevu.Uye.ApplicationUserId,
                    baslik: "Randevunuz Onaylandi",
                    mesaj: $"{randevu.BaslangicZamani:dd.MM.yyyy HH:mm} tarihli randevunuz onaylandi.",
                    tur: "AppointmentApproved",
                    iliskiliId: randevu.Id,
                    link: "/Randevu"
                );
            }

            TempData["Success"] = "Randevu onaylandi.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IptalEt(int id)
        {
            var salon = await GetManagedSalonAsync();
            if (salon == null) return Forbid();

            var randevu = await _context.Randevular
                .Include(r => r.Uye)
                .ThenInclude(u => u!.ApplicationUser)
                .FirstOrDefaultAsync(r => r.Id == id && r.SalonId == salon.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadi veya bu subeye ait degil.";
                return RedirectToAction(nameof(Index));
            }

            randevu.Durum = "Iptal";
            await _context.SaveChangesAsync();

            // Notify the member
            if (randevu.Uye?.ApplicationUserId != null)
            {
                await _bildirimService.OlusturAsync(
                    userId: randevu.Uye.ApplicationUserId,
                    baslik: "Randevunuz Iptal Edildi",
                    mesaj: $"{randevu.BaslangicZamani:dd.MM.yyyy HH:mm} tarihli randevunuz iptal edildi.",
                    tur: "AppointmentCancelled",
                    iliskiliId: randevu.Id,
                    link: "/Randevu"
                );
            }

            TempData["Success"] = "Randevu iptal edildi.";
            return RedirectToAction(nameof(Index));
        }
    }
}
