using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    [Authorize(Policy = "MemberOnly")]
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBildirimService _bildirimService;

        // Randevular arasında bırakılacak minimum ara (dk)
        private const int MinAraDakika = 10;

        public RandevuController(
            AppDbContext context, 
            UserManager<ApplicationUser> userManager,
            IBildirimService bildirimService)
        {
            _context = context;
            _userManager = userManager;
            _bildirimService = bildirimService;
        }

        // Login olan kullanıcının Uye kaydını, ApplicationUserId üzerinden bul
        private async Task<Uye?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return null;

            var uye = await _context.Uyeler
                .Include(u => u.Uyelikler!)
                    .ThenInclude(u => u.Salon)
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            return uye;
        }

        // Dropdown’lar için helper (üyenin aktif olduğu şubeleri doldur)
        private async Task DoldurSelectListelerAsync(
            Uye uye,
            int? salonId = null,
            int? hizmetId = null,
            int? egitmenId = null)
        {
            // Üyenin aktif üyelikleri olan şube id’leri
            var aktifSalonIdler = await _context.Uyelikler
                .Where(x => x.UyeId == uye.Id && x.Durum == "Aktif")
                .Select(x => x.SalonId)
                .Distinct()
                .ToListAsync();

            var salonQuery = _context.Salonlar.AsQueryable();

            if (aktifSalonIdler.Any())
            {
                salonQuery = salonQuery.Where(s => aktifSalonIdler.Contains(s.Id));
            }
            else
            {
                // Hiç aktif salon yoksa, liste boş gelsin
                salonQuery = salonQuery.Where(s => false);
            }

            var salonlar = await salonQuery
                .OrderBy(s => s.Ad)
                .ToListAsync();

            var hizmetler = await _context.Hizmetler
                .OrderBy(h => h.Ad)
                .ToListAsync();

            var egitmenler = await _context.Egitmenler
                .Where(e => e.Aktif)
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            ViewData["SalonId"] = new SelectList(salonlar, "Id", "Ad", salonId);
            ViewData["HizmetId"] = new SelectList(hizmetler, "Id", "Ad", hizmetId);
            ViewData["EgitmenId"] = new SelectList(egitmenler, "Id", "AdSoyad", egitmenId);
        }

        // GET: /Randevu
        // Üyenin kendi randevularını tarih / eğitmen / hizmet filtresi ile listele
        public async Task<IActionResult> Index(DateTime? tarih, int? egitmenId, int? hizmetId)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return View(Enumerable.Empty<Randevu>());
            }

            var query = _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .Where(r => r.UyeId == uye.Id);

            // ===== GEÇMİŞ ONAYLI RANDEVULARI "YAPILDI" OLARAK GÜNCELLE =====
            var now = DateTime.Now;
            var gecmisOnaylilar = await _context.Randevular
                .Where(r => r.UyeId == uye.Id && 
                            r.Durum == "Onaylandı" && 
                            r.BitisZamani < now)
                .ToListAsync();

            if (gecmisOnaylilar.Any())
            {
                foreach (var r in gecmisOnaylilar)
                {
                    r.Durum = "Yapıldı";
                }
                await _context.SaveChangesAsync();
            }

            if (tarih.HasValue)
            {
                var d = tarih.Value.Date;
                query = query.Where(r => r.BaslangicZamani.Date == d);
            }

            if (egitmenId.HasValue)
                query = query.Where(r => r.EgitmenId == egitmenId.Value);

            if (hizmetId.HasValue)
                query = query.Where(r => r.HizmetId == hizmetId.Value);

            var liste = await query
                .OrderByDescending(r => r.BaslangicZamani)
                .ToListAsync();

            // Üyenin aktif üyelikleri olan şube id'leri
            var aktifSalonIdler = await _context.Uyelikler
                .Where(x => x.UyeId == uye.Id && x.Durum == "Aktif")
                .Select(x => x.SalonId)
                .Distinct()
                .ToListAsync();

            // Sadece üyenin aktif olduğu salonlardaki eğitmenler
            var egitmenler = await _context.Egitmenler
                .Where(e => e.Aktif && e.SalonId.HasValue && aktifSalonIdler.Contains(e.SalonId.Value))
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();

            // Filtre dropdown'ları
            ViewData["EgitmenId"] = new SelectList(egitmenler, "Id", "AdSoyad", egitmenId);

            ViewData["HizmetId"] = new SelectList(
                await _context.Hizmetler.OrderBy(h => h.Ad).ToListAsync(),
                "Id", "Ad", hizmetId
            );

            ViewData["SeciliTarih"] = tarih?.ToString("yyyy-MM-dd");

            return View(liste);
        }

        // GET: /Randevu/GetDefaultDateTime
        // Akıllı default tarih hesaplama - Now + 3 saat, salon çalışma saatlerine göre
        [HttpGet]
        public async Task<IActionResult> GetDefaultDateTime(int? salonId)
        {
            var now = DateTime.Now;
            var defaultStart = now.AddHours(3);

            // Salon belirtilmişse çalışma saatlerini kontrol et
            if (salonId.HasValue)
            {
                var salon = await _context.Salonlar.FindAsync(salonId.Value);
                if (salon != null && !salon.Is24Hours && salon.AcilisSaati.HasValue && salon.KapanisSaati.HasValue)
                {
                    var acilis = salon.AcilisSaati.Value;
                    var kapanis = salon.KapanisSaati.Value;
                    var defaultTime = defaultStart.TimeOfDay;

                    // Eğer hesaplanan saat kapanış saatinden sonra ise
                    if (defaultTime >= kapanis)
                    {
                        // Yarın açılış saatine ayarla
                        defaultStart = defaultStart.Date.AddDays(1).Add(acilis);
                    }
                    // Eğer hesaplanan saat açılış saatinden önce ise
                    else if (defaultTime < acilis)
                    {
                        // Bugün açılış saatine ayarla (zaten 3 saat eklenmişti)
                        defaultStart = defaultStart.Date.Add(acilis);
                    }
                }
            }
            else
            {
                // Salon seçilmemişse default olarak ilk salonun çalışma saatlerini kullan
                var ilkSalon = await _context.Salonlar.FirstOrDefaultAsync();
                if (ilkSalon != null && !ilkSalon.Is24Hours && ilkSalon.AcilisSaati.HasValue && ilkSalon.KapanisSaati.HasValue)
                {
                    var acilis = ilkSalon.AcilisSaati.Value;
                    var kapanis = ilkSalon.KapanisSaati.Value;
                    var defaultTime = defaultStart.TimeOfDay;

                    if (defaultTime >= kapanis)
                    {
                        defaultStart = defaultStart.Date.AddDays(1).Add(acilis);
                    }
                    else if (defaultTime < acilis)
                    {
                        defaultStart = defaultStart.Date.Add(acilis);
                    }
                }
            }

            return Json(new { 
                start = defaultStart.ToString("yyyy-MM-ddTHH:mm"),
                formatted = defaultStart.ToString("dd/MM/yyyy HH:mm")
            });
        }

        // GET: /Randevu/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // Bu üyenin en az bir aktif şubesi var mı?
            bool aktifUyelikVar = await _context.Uyelikler.AnyAsync(u =>
                u.UyeId == uye.Id && u.Durum == "Aktif");

            if (!aktifUyelikVar)
            {
                TempData["Error"] = "Randevu oluşturabilmek için en az bir şubede aktif üyeliğiniz olması gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            await DoldurSelectListelerAsync(uye);

            // BaslangicZamani boş bırakılacak - JS tarafından doldurulacak
            var model = new Randevu();

            return View(model);
        }

        // POST: /Randevu/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Randevu randevu)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı. Lütfen yetkili ile iletişime geçin.";
                return RedirectToAction(nameof(Index));
            }

            await DoldurSelectListelerAsync(uye, randevu.SalonId, randevu.HizmetId, randevu.EgitmenId);

            // Temel model doğrulaması
            if (!ModelState.IsValid)
            {
                return View(randevu);
            }

            // Hizmeti bul – süre hesaplamak için
            var hizmet = await _context.Hizmetler
                .FirstOrDefaultAsync(h => h.Id == randevu.HizmetId);

            if (hizmet == null)
            {
                ModelState.AddModelError(string.Empty, "Seçilen hizmet bulunamadı.");
                return View(randevu);
            }

            var baslangic = randevu.BaslangicZamani;
            var bitis = baslangic.AddMinutes(hizmet.SureDakika);

            randevu.BitisZamani = bitis;
            randevu.UyeId = uye.Id;            // güvenlik için
            randevu.Durum = "Beklemede";

            // ---------------- 0) Minimum 3 saat öncesi kontrolü ----------------
            var minZaman = DateTime.Now.AddHours(3);
            if (baslangic < minZaman)
            {
                var minSaatStr = minZaman.ToString("dd.MM.yyyy HH:mm");
                ModelState.AddModelError(string.Empty,
                    $"Randevu en erken 3 saat sonrası için alınabilir. En erken: {minSaatStr}");
            }

            // TimeOfDay değerleri tüm saat kontrollerinde kullanılacak
            var baslangicTime = baslangic.TimeOfDay;
            var bitisTime = bitis.TimeOfDay;

            // ---------------- 0.5) Salon çalışma saatleri kontrolü ----------------
            var salon = await _context.Salonlar.FindAsync(randevu.SalonId);
            if (salon != null && !salon.Is24Hours)
            {
                if (salon.AcilisSaati.HasValue && salon.KapanisSaati.HasValue)
                {
                    if (baslangicTime < salon.AcilisSaati.Value)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Randevu başlangıç saati salonun açılış saatinden ({salon.AcilisSaati.Value:hh\\:mm}) önce olamaz.");
                    }
                    if (bitisTime > salon.KapanisSaati.Value)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Randevu bitiş saati salonun kapanış saatinden ({salon.KapanisSaati.Value:hh\\:mm}) sonra olamaz.");
                    }
                }
            }

            // ---------------- 1) Eğitmenin müsaitliği ----------------
            var gun = baslangic.DayOfWeek;

            bool musaitlikVar = await _context.Musaitlikler.AnyAsync(m =>
                m.EgitmenId == randevu.EgitmenId &&
                m.Gun == gun &&
                m.BaslangicSaati <= baslangicTime &&
                m.BitisSaati >= bitisTime
            );

            if (!musaitlikVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Seçilen eğitmen bu tarih ve saatte çalışmıyor. Lütfen eğitmenin müsait olduğu bir zaman seçin.");
            }

            // ---------------- 2) Aynı eğitmen için çakışan randevu var mı? ----------------
            var ayniGundekiRandevular = await _context.Randevular
                .Where(r =>
                    r.EgitmenId == randevu.EgitmenId &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool egitmenCakismaVar = ayniGundekiRandevular.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (egitmenCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında seçilen eğitmenin başka bir randevusu zaten var.");
            }

            // ---------------- 3) Aynı üyenin aynı saat aralığında başka randevusu var mı? ----------------
            var uyeRandevularAyniGun = await _context.Randevular
                .Where(r =>
                    r.UyeId == uye.Id &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool uyeCakismaVar = uyeRandevularAyniGun.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (uyeCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında size ait başka bir randevu zaten var. " +
                    "Aynı anda birden fazla eğitmenden randevu alınamaz.");
            }

            // ---------------- 4) Arka arkaya randevu yok kuralı (eğitmen için) ----------------
            bool araYok = ayniGundekiRandevular.Any(r =>
                Math.Abs((r.BaslangicZamani - bitis).TotalMinutes) < MinAraDakika ||
                Math.Abs((baslangic - r.BitisZamani).TotalMinutes) < MinAraDakika);

            if (araYok)
            {
                ModelState.AddModelError(string.Empty,
                    $"Randevular arasında en az {MinAraDakika} dakika ara olmalıdır.");
            }

            // Hata varsa formu geri göster
            if (!ModelState.IsValid)
            {
                return View(randevu);
            }

            _context.Randevular.Add(randevu);
            await _context.SaveChangesAsync();

            // Eğitmen bilgisini al
            var egitmen = await _context.Egitmenler.FindAsync(randevu.EgitmenId);
            var egitmenAd = egitmen?.AdSoyad ?? "Eğitmen";

            // ========== YENİ RANDEVU (BEKLEMEDE) - ADMİN'LERE BİLDİRİM ==========
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsers)
            {
                await _bildirimService.OlusturAsync(
                    userId: admin.Id,
                    baslik: "Yeni randevu (Onay bekliyor)",
                    mesaj: $"{uye.AdSoyad} - {egitmenAd} - {randevu.BaslangicZamani:dd.MM.yyyy HH:mm}",
                    tur: "NewAppointmentPending",
                    iliskiliId: randevu.Id,
                    link: "/Admin/Randevu?durum=Beklemede"
                );
            }

            TempData["Success"] = "Randevu talebiniz oluşturuldu. Onaylandığında durum bilgisini bu ekrandan görebilirsiniz.";

            return RedirectToAction(nameof(Index));
        }

        // Üye kendi randevusunu iptal edebilsin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IptalEt(int id)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Randevu ve eğitmen bilgilerini al (bildirim için)
            var randevuBilgi = await _context.Randevular
                .Include(r => r.Egitmen)
                .Include(r => r.Uye)
                .FirstOrDefaultAsync(r => r.Id == id && r.UyeId == uye.Id);

            if (randevuBilgi == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (randevuBilgi.Durum == "İptal")
            {
                TempData["Info"] = "Bu randevu zaten iptal edilmiş.";
                return RedirectToAction(nameof(Index));
            }

            var egitmenAd = randevuBilgi.Egitmen?.AdSoyad ?? "Eğitmen";
            var randevuTarih = randevuBilgi.BaslangicZamani.ToString("dd.MM.yyyy HH:mm");

            randevuBilgi.Durum = "İptal";
            await _context.SaveChangesAsync();

            // ========== RANDEVU İPTAL EDİLDİ - ADMİN'LERE BİLDİRİM ==========
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsers)
            {
                await _bildirimService.OlusturAsync(
                    userId: admin.Id,
                    baslik: "Randevu iptal edildi",
                    mesaj: $"{uye.AdSoyad} - {egitmenAd} - {randevuTarih}",
                    tur: "AppointmentCancelledByUser",
                    iliskiliId: randevuBilgi.Id,
                    link: $"/Admin/Randevu/Details/{randevuBilgi.Id}"
                );
            }

            TempData["Success"] = "Randevu başarıyla iptal edildi.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Randevu/Detail/{id}
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var randevu = await _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .FirstOrDefaultAsync(r => r.Id == id && r.UyeId == uye.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            return View(randevu);
        }

        // GET: /Randevu/Edit/{id}
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var randevu = await _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .FirstOrDefaultAsync(r => r.Id == id && r.UyeId == uye.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (randevu.Durum == "İptal")
            {
                TempData["Error"] = "İptal edilmiş randevular düzenlenemez.";
                return RedirectToAction(nameof(Index));
            }

            if (randevu.Durum == "Yapıldı")
            {
                TempData["Error"] = "Tamamlanmış randevular düzenlenemez.";
                return RedirectToAction(nameof(Index));
            }

            // Eğer onaylandıysa uyarı göster
            if (randevu.Durum == "Onaylandı")
            {
                TempData["Warning"] = "Bu randevu onaylanmış durumda. Düzenleme yaparsanız randevu 'Beklemede' durumuna düşecek ve yeniden admin onayı gerekecek.";
            }

            await DoldurSelectListelerAsync(uye, randevu.SalonId, randevu.HizmetId, randevu.EgitmenId);
            ViewData["EskiDurum"] = randevu.Durum;
            
            return View(randevu);
        }

        // POST: /Randevu/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Randevu model)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde üye kaydınız bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var randevu = await _context.Randevular
                .FirstOrDefaultAsync(r => r.Id == id && r.UyeId == uye.Id);

            if (randevu == null)
            {
                TempData["Error"] = "Randevu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            if (randevu.Durum == "İptal")
            {
                TempData["Error"] = "İptal edilmiş randevular düzenlenemez.";
                return RedirectToAction(nameof(Index));
            }

            if (randevu.Durum == "Yapıldı")
            {
                TempData["Error"] = "Tamamlanmış randevular düzenlenemez.";
                return RedirectToAction(nameof(Index));
            }

            await DoldurSelectListelerAsync(uye, model.SalonId, model.HizmetId, model.EgitmenId);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Hizmeti bul – süre hesaplamak için
            var hizmet = await _context.Hizmetler.FirstOrDefaultAsync(h => h.Id == model.HizmetId);
            if (hizmet == null)
            {
                ModelState.AddModelError(string.Empty, "Seçilen hizmet bulunamadı.");
                return View(model);
            }

            var baslangic = model.BaslangicZamani;
            var bitis = baslangic.AddMinutes(hizmet.SureDakika);

            // TimeOfDay değerleri tüm saat kontrollerinde kullanılacak
            var baslangicTime = baslangic.TimeOfDay;
            var bitisTime = bitis.TimeOfDay;

            // ---------------- 0) Minimum 3 saat öncesi kontrolü ----------------
            var minZaman = DateTime.Now.AddHours(3);
            if (baslangic < minZaman)
            {
                var minSaatStr = minZaman.ToString("dd.MM.yyyy HH:mm");
                ModelState.AddModelError(string.Empty,
                    $"Randevu en erken 3 saat sonrası için alınabilir. En erken: {minSaatStr}");
            }

            // ---------------- 0.5) Salon çalışma saatleri kontrolü ----------------
            var salon = await _context.Salonlar.FindAsync(model.SalonId);
            if (salon != null && !salon.Is24Hours)
            {
                if (salon.AcilisSaati.HasValue && salon.KapanisSaati.HasValue)
                {
                    if (baslangicTime < salon.AcilisSaati.Value)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Randevu başlangıç saati salonun açılış saatinden ({salon.AcilisSaati.Value:hh\\:mm}) önce olamaz.");
                    }
                    if (bitisTime > salon.KapanisSaati.Value)
                    {
                        ModelState.AddModelError(string.Empty,
                            $"Randevu bitiş saati salonun kapanış saatinden ({salon.KapanisSaati.Value:hh\\:mm}) sonra olamaz.");
                    }
                }
            }

            // ---------------- 1) Eğitmenin müsaitliği ----------------
            var gun = baslangic.DayOfWeek;

            bool musaitlikVar = await _context.Musaitlikler.AnyAsync(m =>
                m.EgitmenId == model.EgitmenId &&
                m.Gun == gun &&
                m.BaslangicSaati <= baslangicTime &&
                m.BitisSaati >= bitisTime
            );

            if (!musaitlikVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Seçilen eğitmen bu tarih ve saatte çalışmıyor. Lütfen eğitmenin müsait olduğu bir zaman seçin.");
            }

            // ---------------- 2) Aynı eğitmen için çakışan randevu var mı? (Mevcut randevu hariç) ----------------
            var ayniGundekiRandevular = await _context.Randevular
                .Where(r =>
                    r.Id != id &&  // Düzenlenen randevuyu hariç tut
                    r.EgitmenId == model.EgitmenId &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool egitmenCakismaVar = ayniGundekiRandevular.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (egitmenCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında seçilen eğitmenin başka bir randevusu zaten var.");
            }

            // ---------------- 3) Aynı üyenin aynı saat aralığında başka randevusu var mı? (Mevcut randevu hariç) ----------------
            var uyeRandevularAyniGun = await _context.Randevular
                .Where(r =>
                    r.Id != id &&  // Düzenlenen randevuyu hariç tut
                    r.UyeId == uye.Id &&
                    r.BaslangicZamani.Date == baslangic.Date &&
                    r.Durum != "İptal")
                .ToListAsync();

            bool uyeCakismaVar = uyeRandevularAyniGun.Any(r =>
                !(bitis <= r.BaslangicZamani || baslangic >= r.BitisZamani));

            if (uyeCakismaVar)
            {
                ModelState.AddModelError(string.Empty,
                    "Bu saat aralığında size ait başka bir randevu zaten var. " +
                    "Aynı anda birden fazla eğitmenden randevu alınamaz.");
            }

            // ---------------- 4) Arka arkaya randevu yok kuralı (eğitmen için) ----------------
            bool araYok = ayniGundekiRandevular.Any(r =>
                Math.Abs((r.BaslangicZamani - bitis).TotalMinutes) < MinAraDakika ||
                Math.Abs((baslangic - r.BitisZamani).TotalMinutes) < MinAraDakika);

            if (araYok)
            {
                ModelState.AddModelError(string.Empty,
                    $"Randevular arasında en az {MinAraDakika} dakika ara olmalıdır.");
            }

            // Hata varsa formu geri göster
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var eskiDurum = randevu.Durum;

            // Randevu bilgilerini güncelle
            randevu.SalonId = model.SalonId;
            randevu.HizmetId = model.HizmetId;
            randevu.EgitmenId = model.EgitmenId;
            randevu.BaslangicZamani = baslangic;
            randevu.BitisZamani = bitis;

            // Eğer önceki durum "Onaylandı" ise, düzenleme sonrası "Beklemede" yap
            if (eskiDurum == "Onaylandı")
            {
                randevu.Durum = "Beklemede";
                TempData["Info"] = "Randevu düzenlendi. Durum 'Beklemede' olarak güncellendi, admin onayı bekleniyor.";

                // Admin'lere bildirim gönder
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                foreach (var admin in adminUsers)
                {
                    await _bildirimService.OlusturAsync(
                        userId: admin.Id,
                        baslik: "Randevu düzenlendi (Yeniden onay gerekli)",
                        mesaj: $"{uye.AdSoyad} - {randevu.BaslangicZamani:dd.MM.yyyy HH:mm}",
                        tur: "AppointmentEditedNeedsApproval",
                        iliskiliId: randevu.Id,
                        link: "/Admin/Randevu?durum=Beklemede"
                    );
                }
            }
            else
            {
                TempData["Success"] = "Randevu başarıyla düzenlendi.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Randevu/Calendar
        // FullCalendar.js ile takvim görünümü
        [HttpGet]
        public IActionResult Calendar()
        {
            return View();
        }

        // GET: /Randevu/CalendarEvents
        // FullCalendar.js için JSON event endpoint'i
        [HttpGet]
        public async Task<IActionResult> CalendarEvents()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                return Json(new List<object>());
            }

            var events = await _context.Randevular
                .Where(r => r.UyeId == uye.Id)
                .Include(r => r.Hizmet)
                .Include(r => r.Egitmen)
                .Include(r => r.Salon)
                .Select(r => new
                {
                    id = r.Id,
                    title = r.Hizmet != null ? r.Hizmet.Ad : "Randevu",
                    start = r.BaslangicZamani.ToString("yyyy-MM-ddTHH:mm:ss"),
                    end = r.BitisZamani.ToString("yyyy-MM-ddTHH:mm:ss"),
                    // Durum renkleri: Yeşil=Onaylandı, Sarı=Beklemede, Kırmızı=İptal, Mavi=Yapıldı
                    color = r.Durum == "Onaylandı" ? "#10b981" : 
                            r.Durum == "Beklemede" ? "#f59e0b" : 
                            r.Durum == "Yapıldı" ? "#3b82f6" : "#ef4444",
                    extendedProps = new
                    {
                        durum = r.Durum,
                        egitmen = r.Egitmen != null ? r.Egitmen.AdSoyad : "",
                        salon = r.Salon != null ? r.Salon.Ad : "",
                        hizmetSure = r.Hizmet != null ? r.Hizmet.SureDakika : 0
                    }
                })
                .ToListAsync();

            return Json(events);
        }

        // GET: /Randevu/Egitmenlerim
        // Üyenin aktif şubelerindeki eğitmenleri listele (REST API ile)
        [HttpGet]
        public async Task<IActionResult> Egitmenlerim()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            ViewBag.UyeId = uye.Id;
            return View();
        }
    }
}

