using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Areas.Admin.Controllers
{
    /// <summary>
    /// Admin Destek Kutusu (Support Inbox) Controller
    /// </summary>
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DestekController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IBildirimService _bildirimService;
        private readonly ILogger<DestekController> _logger;

        // Admin email adresi (sabit)
        private const string AdminEmail = "selim.altin@ogr.sakarya.edu.tr";

        public DestekController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IBildirimService bildirimService,
            ILogger<DestekController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _bildirimService = bildirimService;
            _logger = logger;
        }

        /// <summary>
        /// Destek Kutusu - Tüm destek taleplerini listele
        /// </summary>
        public async Task<IActionResult> Index(string durum = "all")
        {
            var query = _context.SupportTickets
                .Include(t => t.User)
                .OrderByDescending(t => t.OlusturulmaTarihi)
                .AsQueryable();

            // Durum filtresi
            if (durum != "all")
            {
                query = query.Where(t => t.Durum == durum);
            }

            var tickets = await query.ToListAsync();

            // İstatistikler
            ViewBag.ToplamAcik = await _context.SupportTickets.CountAsync(t => t.Durum == "Open");
            ViewBag.ToplamYanitlandi = await _context.SupportTickets.CountAsync(t => t.Durum == "Closed");
            ViewBag.ToplamTalep = await _context.SupportTickets.CountAsync();
            ViewBag.AktifDurum = durum;

            return View(tickets);
        }

        /// <summary>
        /// Destek talebi detayı ve yanıt formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.Admin)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        /// <summary>
        /// Admin yanıtı gönder ve ticket'ı kapat
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(DestekYanitViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var ticket = await _context.SupportTickets
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == model.TicketId);
                return View("Details", ticket);
            }

            var supportTicket = await _context.SupportTickets.FindAsync(model.TicketId);
            if (supportTicket == null)
            {
                return NotFound();
            }

            // Zaten kapalı mı kontrol et
            if (supportTicket.Durum == "Closed")
            {
                TempData["ErrorMessage"] = "Bu destek talebi zaten yanıtlanmış ve kapatılmış.";
                return RedirectToAction("Details", new { id = model.TicketId });
            }

            var admin = await _userManager.GetUserAsync(User);

            // Ticket güncelle
            supportTicket.AdminCevap = model.AdminCevap;
            supportTicket.CevapTarihi = DateTime.UtcNow;
            supportTicket.AdminId = admin?.Id;
            supportTicket.Durum = "Closed";

            // Kullanıcıya bildirim oluştur (site içi)
            if (!string.IsNullOrEmpty(supportTicket.UserId))
            {
                await _bildirimService.OlusturAsync(
                    userId: supportTicket.UserId,
                    baslik: $"Destek Talebiniz Yanıtlandı (#{supportTicket.Id})",
                    mesaj: model.AdminCevap.Length > 200 ? model.AdminCevap.Substring(0, 200) + "..." : model.AdminCevap,
                    tur: "DestekYaniti",
                    iliskiliId: supportTicket.Id,
                    link: $"/Help/Details/{supportTicket.Id}"
                );
            }

            // Kullanıcıya email gönder
            bool mailBasarili = false;
            if (_emailService.IsConfigured)
            {
                try
                {
                    var subject = $"[Fitness Center] Destek Talebinize Yanıt - #{supportTicket.Id}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #4f46e5;'>Destek Talebinize Yanıt</h2>
                            <p>Sayın {supportTicket.KullaniciAdi ?? "Değerli Kullanıcımız"},</p>
                            <p>#{supportTicket.Id} numaralı destek talebiniz yanıtlandı.</p>
                            
                            <div style='background-color: #f3f4f6; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                                <p><strong>Talebiniz:</strong></p>
                                <p style='color: #6b7280;'>{supportTicket.Mesaj}</p>
                            </div>
                            
                            <div style='background-color: #e0e7ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                                <p><strong>Yanıtımız:</strong></p>
                                <p>{model.AdminCevap}</p>
                            </div>
                            
                            <p>Başka sorularınız varsa lütfen yeni bir destek talebi oluşturun.</p>
                            <p>Saygılarımızla,<br><strong>Fitness Center Destek Ekibi</strong></p>
                        </div>
                    ";

                    mailBasarili = await _emailService.SendAsync(supportTicket.Email, subject, body);
                    supportTicket.KullaniciMailGonderildi = mailBasarili;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Kullanıcıya yanıt maili gönderilemedi: Ticket #{TicketId}", supportTicket.Id);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Destek talebi yanıtlandı: Ticket #{TicketId}, Admin: {AdminId}, Mail: {MailDurumu}",
                supportTicket.Id, admin?.Id, mailBasarili ? "Gönderildi" : "Gönderilemedi");

            TempData["SuccessMessage"] = mailBasarili
                ? "Yanıtınız kaydedildi ve kullanıcıya mail olarak gönderildi."
                : "Yanıtınız kaydedildi. (Mail gönderilemedi - kullanıcı siteden görebilir)";

            return RedirectToAction("Index");
        }

        /// <summary>
        /// Ticket silme (sadece gerekirse)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            _context.SupportTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Destek talebi silindi.";
            return RedirectToAction("Index");
        }
    }
}
