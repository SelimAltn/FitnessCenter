using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class KullaniciController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public KullaniciController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Kullanici
        public async Task<IActionResult> Index()
        {
            // Tüm kullanıcıları çek
            var users = await _context.Users
                .OrderByDescending(u => u.Id)
                .ToListAsync();

            // Her kullanıcı için üyelik sayısını hesapla (N+1 sorgusu önlemek için önceden çek)
            var uyeler = await _context.Uyeler
                .Include(u => u.Uyelikler)
                .Where(u => !string.IsNullOrEmpty(u.ApplicationUserId))
                .ToListAsync();

            var uyeDict = uyeler.ToDictionary(u => u.ApplicationUserId!, u => u);

            var model = new List<KullaniciListeViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var uye = uyeDict.GetValueOrDefault(user.Id);
                var uyelikSayisi = uye?.Uyelikler?.Count(x => x.Durum == "Aktif") ?? 0;

                model.Add(new KullaniciListeViewModel
                {
                    Id = user.Id,
                    KullaniciAdi = user.UserName ?? "",
                    Email = user.Email ?? "",
                    Rol = roles.FirstOrDefault() ?? "Member",
                    UyelikDurumu = uyelikSayisi == 0 
                        ? "Üyeliği yok" 
                        : $"{uyelikSayisi} şubede üye",
                    UyeId = uye?.Id
                });
            }

            return View(model);
        }

        // GET: Admin/Kullanici/Details/id
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var uye = await _context.Uyeler
                .Include(u => u.Uyelikler!)
                    .ThenInclude(uy => uy.Salon)
                .FirstOrDefaultAsync(u => u.ApplicationUserId == id);

            var model = new KullaniciDetayViewModel
            {
                Id = user.Id,
                KullaniciAdi = user.UserName ?? "",
                Email = user.Email ?? "",
                Rol = roles.FirstOrDefault() ?? "Member",
                Uye = uye,
                Uyelikler = uye?.Uyelikler?.ToList() ?? new List<Uyelik>()
            };

            return View(model);
        }
    }

    // ViewModel'ler (aynı dosyada tutuyorum basitlik için)
    public class KullaniciListeViewModel
    {
        public string Id { get; set; } = "";
        public string KullaniciAdi { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rol { get; set; } = "";
        public string UyelikDurumu { get; set; } = "";
        public int? UyeId { get; set; }
    }

    public class KullaniciDetayViewModel
    {
        public string Id { get; set; } = "";
        public string KullaniciAdi { get; set; } = "";
        public string Email { get; set; } = "";
        public string Rol { get; set; } = "";
        public Uye? Uye { get; set; }
        public List<Uyelik> Uyelikler { get; set; } = new();
    }
}
