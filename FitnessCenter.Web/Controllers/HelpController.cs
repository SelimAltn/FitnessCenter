using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Controllers
{
    /// <summary>
    /// Kullanıcı Help/Destek Controller
    /// - Yeni destek talebi oluşturma
    /// - Mevcut talepleri ve yanıtları görüntüleme
    /// </summary>
    [Authorize]
    public class HelpController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IBildirimService _bildirimService;
        private readonly ILogger<HelpController> _logger;

        // Admin email adresi (sabit)
        private const string AdminEmail = "selim.altin@ogr.sakarya.edu.tr";

        public HelpController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IBildirimService bildirimService,
            ILogger<HelpController> logger)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _bildirimService = bildirimService;
            _logger = logger;
        }

        /// <summary>
        /// Yardım sayfası - Yeni destek talebi formu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var model = new HelpViewModel
            {
                Email = user?.Email ?? string.Empty
            };
            return View(model);
        }

        /// <summary>
        /// Yeni destek talebi gönder
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(HelpViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            // Support ticket oluştur
            var ticket = new SupportTicket
            {
                UserId = user?.Id,
                KullaniciAdi = user?.UserName,
                Konu = model.Konu,
                Mesaj = model.Mesaj,
                Email = model.Email,
                OlusturulmaTarihi = DateTime.UtcNow,
                Durum = "Open"
            };

            _context.SupportTickets.Add(ticket);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Yeni destek talebi oluşturuldu: Ticket #{TicketId}, Kullanıcı: {UserId}", ticket.Id, user?.Id);

            // ========== TÜM ADMİN'LERE BİLDİRİM GÖNDER ==========
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            foreach (var admin in adminUsers)
            {
                await _bildirimService.OlusturAsync(
                    userId: admin.Id,
                    baslik: $"Yeni Destek Talebi #{ticket.Id}",
                    mesaj: $"{user?.UserName ?? "Bilinmiyor"}: {model.Konu}",
                    tur: "YeniDestekTalebi",
                    iliskiliId: ticket.Id,
                    link: $"/Admin/Destek/Details/{ticket.Id}"
                );
            }

            // Admin'e email gönder
            bool mailBasarili = false;
            if (_emailService.IsConfigured)
            {
                try
                {
                    var subject = $"[Fitness Center] Yeni Destek Talebi #{ticket.Id}: {model.Konu}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                            <h2 style='color: #4f46e5;'>Yeni Destek Talebi</h2>
                            
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <tr>
                                    <td style='padding: 10px; background-color: #f3f4f6; font-weight: bold; width: 150px;'>Talep No:</td>
                                    <td style='padding: 10px; background-color: #f9fafb;'>#{ticket.Id}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; background-color: #f3f4f6; font-weight: bold;'>Kullanıcı:</td>
                                    <td style='padding: 10px; background-color: #f9fafb;'>{user?.UserName ?? "Bilinmiyor"}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; background-color: #f3f4f6; font-weight: bold;'>E-posta:</td>
                                    <td style='padding: 10px; background-color: #f9fafb;'>{model.Email}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; background-color: #f3f4f6; font-weight: bold;'>Konu:</td>
                                    <td style='padding: 10px; background-color: #f9fafb;'>{model.Konu}</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; background-color: #f3f4f6; font-weight: bold;'>Tarih:</td>
                                    <td style='padding: 10px; background-color: #f9fafb;'>{ticket.OlusturulmaTarihi:dd.MM.yyyy HH:mm}</td>
                                </tr>
                            </table>
                            
                            <div style='background-color: #e0e7ff; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                                <p><strong>Mesaj:</strong></p>
                                <p>{model.Mesaj}</p>
                            </div>
                            
                            <p style='color: #6b7280; font-size: 14px;'>
                                Bu talebi yanıtlamak için <a href='https://localhost/Admin/Destek/Details/{ticket.Id}'>Admin Paneli</a>ni ziyaret edin.
                            </p>
                        </div>
                    ";

                    mailBasarili = await _emailService.SendAsync(AdminEmail, subject, body);
                    ticket.AdminMailGonderildi = mailBasarili;
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Admin'e destek talebi maili gönderilemedi: Ticket #{TicketId}", ticket.Id);
                }
            }

            TempData["SuccessMessage"] = "Destek talebiniz başarıyla gönderildi. En kısa sürede size dönüş yapacağız.";
            return RedirectToAction("Inbox");
        }

        /// <summary>
        /// Kullanıcının destek talepleri ve yanıtları (Gelen Kutusu)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var tickets = await _context.SupportTickets
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.OlusturulmaTarihi)
                .ToListAsync();

            return View(tickets);
        }

        /// <summary>
        /// Destek talebi detayı (kullanıcı görünümü)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Account");

            var ticket = await _context.SupportTickets
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == user.Id);

            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }
    }
}
