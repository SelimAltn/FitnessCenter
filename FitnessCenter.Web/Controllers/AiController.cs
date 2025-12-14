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
    /// AI tabanlı fitness önerisi controller'ı
    /// Route: /Ai/Recommend
    /// </summary>
    [Authorize(Policy = "MemberOnly")]
    public class AiController : Controller
    {
        private readonly IAiRecommendationService _aiService;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AiController> _logger;

        // İzin verilen foto formatları ve max boyut
        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedPhotoContentTypes = { "image/jpeg", "image/png" };
        private const long MaxPhotoSize = 5 * 1024 * 1024; // 5 MB (artırıldı)

        public AiController(
            IAiRecommendationService aiService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AiController> logger)
        {
            _aiService = aiService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Login olan kullanıcının Uye kaydını, ApplicationUserId üzerinden bulur.
        /// Üye bulunamazsa null döner.
        /// </summary>
        private async Task<Uye?> GetCurrentMemberAsync()
        {
            // 1. Identity kullanıcısını al
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("GetCurrentMemberAsync: User not found from UserManager");
                return null;
            }

            // 2. ApplicationUserId ile eşleşen Uye kaydını bul
            var uye = await _context.Uyeler
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            if (uye == null)
            {
                _logger.LogWarning("GetCurrentMemberAsync: No Uye record found for ApplicationUserId: {UserId}", user.Id);
            }

            return uye;
        }

        /// <summary>
        /// GET: /Ai/Recommend
        /// AI öneri formunu gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Recommend()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı. Önce bir şubeye üye olmanız gerekiyor.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // API yapılandırılmamışsa uyarı göster
            if (!_aiService.IsApiConfigured)
            {
                TempData["Warning"] = "AI servisi şu an yapılandırılmamış durumda. Otomatik öneri sistemi kullanılacaktır.";
            }

            var model = new AiRecommendVm
            {
                AntrenmanGunu = 3, // varsayılan değer
                Hedef = "Fit Kalma",
                Ekipman = "Gym (Salon erişimi)"
            };

            return View(model);
        }

        /// <summary>
        /// POST: /Ai/Recommend
        /// Form gönderildiğinde AI öneri işlemini başlatır
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
        public async Task<IActionResult> Recommend(AiRecommendVm model)
        {
            try
            {
                // 1. Üye kontrolü
                var uye = await GetCurrentMemberAsync();
                if (uye == null)
                {
                    TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı.";
                    return RedirectToAction("UyeOl", "Uyelik");
                }

                // 2. Foto validasyonu (opsiyonel ama varsa doğrula)
                if (model.Photo != null && model.Photo.Length > 0)
                {
                    var photoValidation = ValidatePhoto(model.Photo);
                    if (!photoValidation.isValid)
                    {
                        ModelState.AddModelError(nameof(model.Photo), photoValidation.error!);
                    }
                }

                // 3. Model validasyonu (IValidatableObject dahil)
                if (!ModelState.IsValid)
                {
                    // Validation hatalarını logla
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    _logger.LogWarning("AI Recommend validation failed: {Errors}", string.Join(", ", errors));
                    return View(model);
                }

                // 4. AI servisi çağrısı (cache kontrolü dahil)
                var result = await _aiService.GetRecommendationAsync(model, uye.Id);

                // 5. Sonucu TempData'ya kaydet ve Result sayfasına yönlendir
                TempData["AiResult"] = System.Text.Json.JsonSerializer.Serialize(result);
                
                return RedirectToAction(nameof(Result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI recommendation error - unhandled exception");
                
                // Fallback response oluştur
                var fallbackResult = new AiResultVm
                {
                    IsSuccess = true,
                    IsFallback = true,
                    Summary = "Bir hata oluştu ancak size genel öneriler sunuyoruz.",
                    WorkoutPlan = new List<string>
                    {
                        "Haftada 3-4 gün düzenli egzersiz yapın",
                        "Kardiyo ve güç antrenmanlarını dengeli kombine edin",
                        "Her antrenmandan önce ısınma ve sonra soğuma yapın"
                    },
                    NutritionTips = new List<string>
                    {
                        "Dengeli ve çeşitli beslenin",
                        "Günde en az 2-3 litre su için",
                        "İşlenmiş gıdalardan kaçının"
                    },
                    Warnings = new List<string>
                    {
                        "Bu öneriler genel niteliktedir",
                        "Yeni bir programa başlamadan önce doktorunuza danışın"
                    },
                    ErrorMessage = "Sistem geçici olarak AI servisine ulaşamadı. Otomatik öneriler gösterilmektedir.",
                    GeneratedAt = DateTime.UtcNow,
                    RecommendationType = "Fallback",
                    ModelUsed = "fallback"
                };

                TempData["AiResult"] = System.Text.Json.JsonSerializer.Serialize(fallbackResult);
                return RedirectToAction(nameof(Result));
            }
        }

        /// <summary>
        /// GET: /Ai/Result
        /// AI öneri sonucunu gösterir
        /// </summary>
        [HttpGet]
        public IActionResult Result()
        {
            // TempData'dan sonucu al
            var resultJson = TempData["AiResult"] as string;
            
            if (string.IsNullOrEmpty(resultJson))
            {
                TempData["Error"] = "Görüntülenecek sonuç bulunamadı. Lütfen yeni bir öneri alın.";
                return RedirectToAction(nameof(Recommend));
            }

            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<AiResultVm>(resultJson);
                
                if (result == null)
                {
                    TempData["Error"] = "Sonuç işlenirken bir hata oluştu.";
                    return RedirectToAction(nameof(Recommend));
                }

                return View(result);
            }
            catch
            {
                TempData["Error"] = "Sonuç işlenirken bir hata oluştu.";
                return RedirectToAction(nameof(Recommend));
            }
        }

        /// <summary>
        /// Foto dosyasını doğrular (format ve boyut)
        /// </summary>
        private static (bool isValid, string? error) ValidatePhoto(IFormFile photo)
        {
            // Boyut kontrolü
            if (photo.Length > MaxPhotoSize)
            {
                return (false, "Fotoğraf boyutu en fazla 5 MB olabilir.");
            }

            // Uzantı kontrolü
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(extension))
            {
                return (false, "Sadece JPG ve PNG formatları kabul edilmektedir.");
            }

            // Content-Type kontrolü
            if (!AllowedPhotoContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
            {
                return (false, "Geçersiz dosya formatı. Sadece JPG ve PNG kabul edilmektedir.");
            }

            return (true, null);
        }
    }
}
