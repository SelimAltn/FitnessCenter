using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class EgitmenController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBildirimService _bildirimService;

        public EgitmenController(
            AppDbContext context, 
            UserManager<ApplicationUser> userManager,
            IBildirimService bildirimService)
        {
            _context = context;
            _userManager = userManager;
            _bildirimService = bildirimService;
        }

        // GET: Admin/Egitmen
        public async Task<IActionResult> Index()
        {
            var egitmenler = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .OrderBy(e => e.AdSoyad)
                .ToListAsync();
            return View(egitmenler);
        }

        // GET: Admin/Egitmen/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (egitmen == null) return NotFound();

            return View(egitmen);
        }

        // GET: Admin/Egitmen/Create
        public async Task<IActionResult> Create()
        {
            await PopulateDropdownsAsync();
            
            var model = new EgitmenCreateVm
            {
                CalismaSaatleri = GetDefaultCalismaSaatleri()
            };

            return View(model);
        }

        // POST: Admin/Egitmen/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EgitmenCreateVm model)
        {
            await PopulateDropdownsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Kullanıcı adı benzersiz mi?
            var existingUser = await _userManager.FindByNameAsync(model.KullaniciAdi);
            if (existingUser != null)
            {
                ModelState.AddModelError("KullaniciAdi", "Bu kullanıcı adı zaten kullanılıyor.");
                return View(model);
            }

            // Email benzersiz mi?
            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi zaten kullanılıyor.");
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Identity kullanıcısı oluştur
                var user = new ApplicationUser
                {
                    UserName = model.KullaniciAdi,
                    Email = model.Email,
                    EmailConfirmed = true,
                    ThemePreference = "Light"
                };

                var createResult = await _userManager.CreateAsync(user, model.Sifre);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // Trainer rolü ata
                await _userManager.AddToRoleAsync(user, "Trainer");

                // 2. Egitmen kaydı oluştur
                var egitmen = new Egitmen
                {
                    AdSoyad = model.AdSoyad,
                    Email = model.Email,
                    Telefon = model.Telefon,
                    KullaniciAdi = model.KullaniciAdi,
                    SifreHash = model.Sifre, // Plain text olarak sakla (admin görmek için)
                    SalonId = model.SalonId,
                    Maas = model.Maas,
                    Biyografi = model.Biyografi,
                    ApplicationUserId = user.Id,
                    Aktif = true
                };

                _context.Egitmenler.Add(egitmen);
                await _context.SaveChangesAsync();

                // 3. Uzmanlık alanlarını ekle
                if (model.SecilenUzmanliklar?.Any() == true)
                {
                    foreach (var uzmanlikId in model.SecilenUzmanliklar)
                    {
                        _context.EgitmenUzmanliklari.Add(new EgitmenUzmanlik
                        {
                            EgitmenId = egitmen.Id,
                            UzmanlikAlaniId = uzmanlikId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 3.5 Hizmetleri ekle
                if (model.SecilenHizmetler?.Any() == true)
                {
                    foreach (var hizmetId in model.SecilenHizmetler)
                    {
                        _context.EgitmenHizmetler.Add(new EgitmenHizmet
                        {
                            EgitmenId = egitmen.Id,
                            HizmetId = hizmetId
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                // 4. Çalışma saatlerini ekle
                if (model.CalismaSaatleri?.Any() == true)
                {
                    foreach (var cs in model.CalismaSaatleri.Where(c => c.Calisiyor && c.BaslangicSaati.HasValue && c.BitisSaati.HasValue))
                    {
                        _context.Musaitlikler.Add(new Musaitlik
                        {
                            EgitmenId = egitmen.Id,
                            Gun = cs.Gun,
                            BaslangicSaati = cs.BaslangicSaati!.Value,
                            BitisSaati = cs.BitisSaati!.Value
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                TempData["Success"] = $"Eğitmen '{model.AdSoyad}' başarıyla oluşturuldu.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Eğitmen oluşturulurken bir hata oluştu: " + ex.Message);
                return View(model);
            }
        }

        // GET: Admin/Egitmen/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler
                .Include(e => e.EgitmenUzmanliklari)
                .Include(e => e.EgitmenHizmetler)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (egitmen == null) return NotFound();

            await PopulateDropdownsAsync();

            var model = new EgitmenEditVm
            {
                Id = egitmen.Id,
                AdSoyad = egitmen.AdSoyad,
                Email = egitmen.Email ?? "",
                Telefon = egitmen.Telefon,
                KullaniciAdi = egitmen.KullaniciAdi,
                Sifre = egitmen.SifreHash,
                SalonId = egitmen.SalonId ?? 0,
                Maas = egitmen.Maas,
                Biyografi = egitmen.Biyografi,
                Aktif = egitmen.Aktif,
                SecilenUzmanliklar = egitmen.EgitmenUzmanliklari?.Select(eu => eu.UzmanlikAlaniId).ToList() ?? new List<int>(),
                SecilenHizmetler = egitmen.EgitmenHizmetler?.Select(eh => eh.HizmetId).ToList() ?? new List<int>(),
                CalismaSaatleri = GetCalismaSaatleriFromEgitmen(egitmen)
            };

            return View(model);
        }

        // POST: Admin/Egitmen/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EgitmenEditVm model)
        {
            if (id != model.Id) return NotFound();

            await PopulateDropdownsAsync();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var egitmen = await _context.Egitmenler
                .Include(e => e.EgitmenUzmanliklari)
                .Include(e => e.EgitmenHizmetler)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (egitmen == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Egitmen bilgilerini güncelle
                egitmen.AdSoyad = model.AdSoyad;
                egitmen.Email = model.Email;
                egitmen.Telefon = model.Telefon;
                egitmen.SalonId = model.SalonId;
                egitmen.Maas = model.Maas;
                egitmen.Biyografi = model.Biyografi;
                egitmen.Aktif = model.Aktif;

                // Şifre güncelleme
                if (!string.IsNullOrEmpty(model.Sifre) && model.Sifre != egitmen.SifreHash)
                {
                    egitmen.SifreHash = model.Sifre;
                    
                    // Identity şifresini de güncelle
                    if (!string.IsNullOrEmpty(egitmen.ApplicationUserId))
                    {
                        var user = await _userManager.FindByIdAsync(egitmen.ApplicationUserId);
                        if (user != null)
                        {
                            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                            await _userManager.ResetPasswordAsync(user, token, model.Sifre);
                        }
                    }
                }

                // Uzmanlıkları güncelle - önce mevcut olanları sil
                if (egitmen.EgitmenUzmanliklari != null)
                {
                    _context.EgitmenUzmanliklari.RemoveRange(egitmen.EgitmenUzmanliklari);
                }

                if (model.SecilenUzmanliklar?.Any() == true)
                {
                    foreach (var uzmanlikId in model.SecilenUzmanliklar)
                    {
                        _context.EgitmenUzmanliklari.Add(new EgitmenUzmanlik
                        {
                            EgitmenId = egitmen.Id,
                            UzmanlikAlaniId = uzmanlikId
                        });
                    }
                }

                // Hizmetleri güncelle - önce mevcut olanları sil
                if (egitmen.EgitmenHizmetler != null)
                {
                    _context.EgitmenHizmetler.RemoveRange(egitmen.EgitmenHizmetler);
                }

                if (model.SecilenHizmetler?.Any() == true)
                {
                    foreach (var hizmetId in model.SecilenHizmetler)
                    {
                        _context.EgitmenHizmetler.Add(new EgitmenHizmet
                        {
                            EgitmenId = egitmen.Id,
                            HizmetId = hizmetId
                        });
                    }
                }

                // Çalışma saatlerini güncelle - önce mevcut olanları sil
                if (egitmen.Musaitlikler != null)
                {
                    _context.Musaitlikler.RemoveRange(egitmen.Musaitlikler);
                }

                if (model.CalismaSaatleri?.Any() == true)
                {
                    foreach (var cs in model.CalismaSaatleri.Where(c => c.Calisiyor && c.BaslangicSaati.HasValue && c.BitisSaati.HasValue))
                    {
                        _context.Musaitlikler.Add(new Musaitlik
                        {
                            EgitmenId = egitmen.Id,
                            Gun = cs.Gun,
                            BaslangicSaati = cs.BaslangicSaati!.Value,
                            BitisSaati = cs.BitisSaati!.Value
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Eğitmen '{model.AdSoyad}' başarıyla güncellendi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, "Güncelleme sırasında hata: " + ex.Message);
                return View(model);
            }
        }

        // GET: Admin/Egitmen/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var egitmen = await _context.Egitmenler
                .Include(e => e.Salon)
                .Include(e => e.Randevular)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (egitmen == null) return NotFound();

            ViewData["AktifRandevuSayisi"] = egitmen.Randevular?.Count(r => r.Durum != "İptal") ?? 0;

            return View(egitmen);
        }

        // POST: Admin/Egitmen/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var egitmen = await _context.Egitmenler
                .Include(e => e.Randevular!)
                    .ThenInclude(r => r.Uye!)
                        .ThenInclude(u => u.ApplicationUser)
                .Include(e => e.EgitmenUzmanliklari)
                .Include(e => e.Musaitlikler)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (egitmen == null)
            {
                TempData["Error"] = "Eğitmen bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Randevuları iptal et ve kullanıcılara bildirim gönder
                if (egitmen.Randevular?.Any() == true)
                {
                    foreach (var randevu in egitmen.Randevular.Where(r => r.Durum != "İptal"))
                    {
                        randevu.Durum = "İptal";

                        // Kullanıcıya bildirim
                        var userId = randevu.Uye?.ApplicationUserId;
                        if (!string.IsNullOrEmpty(userId))
                        {
                            await _bildirimService.OlusturAsync(
                                userId: userId,
                                baslik: "Randevunuz iptal edildi",
                                mesaj: $"{egitmen.AdSoyad} isimli eğitmen sistemden kaldırıldığı için {randevu.BaslangicZamani:dd.MM.yyyy HH:mm} tarihli randevunuz iptal edildi.",
                                tur: "AppointmentCancelledTrainerRemoved",
                                iliskiliId: randevu.Id,
                                link: "/Randevu"
                            );
                        }
                    }
                }

                // 2. Uzmanlıkları sil
                if (egitmen.EgitmenUzmanliklari != null)
                {
                    _context.EgitmenUzmanliklari.RemoveRange(egitmen.EgitmenUzmanliklari);
                }

                // 3. Müsaitlikleri sil
                if (egitmen.Musaitlikler != null)
                {
                    _context.Musaitlikler.RemoveRange(egitmen.Musaitlikler);
                }

                // 4. Identity kullanıcısını sil veya deaktif et
                if (!string.IsNullOrEmpty(egitmen.ApplicationUserId))
                {
                    var user = await _userManager.FindByIdAsync(egitmen.ApplicationUserId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);
                    }
                }

                // 5. Egitmeni sil
                _context.Egitmenler.Remove(egitmen);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = $"Eğitmen '{egitmen.AdSoyad}' ve ilişkili kayıtlar silindi. İlgili randevular iptal edildi.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Silme işlemi sırasında hata: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Admin/Egitmen/ToggleAktif/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAktif(int id)
        {
            var egitmen = await _context.Egitmenler.FindAsync(id);
            if (egitmen == null)
            {
                TempData["Error"] = "Eğitmen bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            egitmen.Aktif = !egitmen.Aktif;
            await _context.SaveChangesAsync();

            TempData["Success"] = egitmen.Aktif 
                ? $"'{egitmen.AdSoyad}' aktif edildi." 
                : $"'{egitmen.AdSoyad}' pasif edildi.";

            return RedirectToAction(nameof(Index));
        }

        #region Helper Methods

        private async Task PopulateDropdownsAsync()
        {
            ViewData["Salonlar"] = new SelectList(
                await _context.Salonlar.OrderBy(s => s.Ad).ToListAsync(),
                "Id", "Ad");

            ViewData["UzmanlikAlanlari"] = await _context.UzmanlikAlanlari
                .Where(u => u.Aktif)
                .OrderBy(u => u.Ad)
                .ToListAsync();

            ViewData["Hizmetler"] = await _context.Hizmetler
                .OrderBy(h => h.Ad)
                .ToListAsync();
        }

        private List<CalismaGunuVm> GetDefaultCalismaSaatleri()
        {
            return new List<CalismaGunuVm>
            {
                new() { Gun = DayOfWeek.Monday, Calisiyor = false },
                new() { Gun = DayOfWeek.Tuesday, Calisiyor = false },
                new() { Gun = DayOfWeek.Wednesday, Calisiyor = false },
                new() { Gun = DayOfWeek.Thursday, Calisiyor = false },
                new() { Gun = DayOfWeek.Friday, Calisiyor = false },
                new() { Gun = DayOfWeek.Saturday, Calisiyor = false },
                new() { Gun = DayOfWeek.Sunday, Calisiyor = false }
            };
        }

        private List<CalismaGunuVm> GetCalismaSaatleriFromEgitmen(Egitmen egitmen)
        {
            var result = GetDefaultCalismaSaatleri();

            if (egitmen.Musaitlikler != null)
            {
                foreach (var m in egitmen.Musaitlikler)
                {
                    var gun = result.FirstOrDefault(c => c.Gun == m.Gun);
                    if (gun != null)
                    {
                        gun.Calisiyor = true;
                        gun.BaslangicSaati = m.BaslangicSaati;
                        gun.BitisSaati = m.BitisSaati;
                    }
                }
            }

            return result;
        }

        #endregion
    }
}
