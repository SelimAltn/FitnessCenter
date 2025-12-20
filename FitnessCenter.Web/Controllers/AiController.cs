using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Implementations;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FitnessCenter.Web.Controllers
{
    /// <summary>
    /// AI tabanlı fitness önerisi controller'ı
    /// Data modu: BMI hesapla → DeepSeek
    /// Photo modu: Gemini Vision → DeepSeek
    /// </summary>
    [Authorize(Policy = "MemberOnly")]
    public class AiController : Controller
    {
        private readonly IDeepSeekService _textService;
        private readonly IAiVisionService _visionService;
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AiController> _logger;
        private readonly AppearanceImageMapper _imageMapper;
        private readonly FalImageToImageService _falService;

        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png" };
        private static readonly string[] AllowedPhotoContentTypes = { "image/jpeg", "image/png" };
        private const long MaxPhotoSize = 5 * 1024 * 1024; // 5 MB

        public AiController(
            IDeepSeekService textService,
            IAiVisionService visionService,
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<AiController> logger,
            AppearanceImageMapper imageMapper,
            FalImageToImageService falService)
        {
            _textService = textService;
            _visionService = visionService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _imageMapper = imageMapper;
            _falService = falService;
        }

        private async Task<Uye?> GetCurrentMemberAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Uyeler
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);
        }

        /// <summary>
        /// GET: /Ai/History - Üyenin geçmiş AI öneri kayıtlarını listeler
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> History(string? tip, int page = 1)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            const int pageSize = 10;

            // Temel sorgu
            var query = _context.AiLoglar
                .Where(a => a.UyeId == uye.Id)
                .AsQueryable();

            // Tip filtresi
            if (!string.IsNullOrEmpty(tip))
            {
                if (tip.Equals("Photo", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => a.SoruMetni.StartsWith("Photo"));
                }
                else if (tip.Equals("Data", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(a => !a.SoruMetni.StartsWith("Photo"));
                }
            }

            // Toplam kayıt sayısı
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Sayfa sınırları kontrolü
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            // Veri çek
            var items = await query
                .OrderByDescending(a => a.OlusturulmaZamani)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AiHistoryItemVm
                {
                    Id = a.Id,
                    Tip = a.SoruMetni.StartsWith("Photo") ? "Photo" : "Data",
                    Girdi = a.SoruMetni.Length > 100 ? a.SoruMetni.Substring(0, 100) + "..." : a.SoruMetni,
                    Cevap = a.CevapMetni.Length > 150 ? a.CevapMetni.Substring(0, 150) + "..." : a.CevapMetni,
                    Tarih = a.OlusturulmaZamani,
                    IsSuccess = a.IsSuccess,
                    DurationMs = a.DurationMs
                })
                .ToListAsync();

            var model = new AiHistoryVm
            {
                Items = items,
                TipFilter = tip,
                CurrentPage = page,
                TotalPages = totalPages,
                TotalItems = totalItems,
                PageSize = pageSize
            };

            return View(model);
        }

        /// <summary>
        /// GET: /Ai/Recommend - Form göster
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Recommend()
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // Servis durumunu kontrol et
            if (!_textService.IsConfigured)
            {
                ViewBag.Warning = "AI servisi (DeepSeek) yapılandırılmamış.";
            }
            else if (!_visionService.IsConfigured)
            {
                ViewBag.Warning = "Fotoğraf analizi servisi (Gemini) yapılandırılmamış.";
            }

            ViewBag.VisionEnabled = _visionService.IsConfigured;

            var model = new AiRecommendVm
            {
                Hedef = "Fit Kalma"
            };

            return View(model);
        }

        /// <summary>
        /// POST: /Ai/Recommend
        /// Data modu: BMI → DeepSeek
        /// Photo modu: Gemini Vision → DeepSeek
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Recommend(AiRecommendVm model)
        {
            var stopwatch = Stopwatch.StartNew();

            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // Fotoğraf validasyonu
            if (model.IsPhotoMode && model.Photo != null)
            {
                var photoValidation = ValidatePhoto(model.Photo);
                if (!photoValidation.isValid)
                {
                    ModelState.AddModelError(nameof(model.Photo), photoValidation.error!);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.VisionEnabled = _visionService.IsConfigured;
                return View(model);
            }

            AiResultVm result;

            if (model.IsPhotoMode && model.Photo != null)
            {
                // ===== PHOTO MODU =====
                _logger.LogInformation("Photo mode request from UyeId: {UyeId}", uye.Id);

                // 1. Fotoğrafı byte array'e çevir
                using var memoryStream = new MemoryStream();
                await model.Photo.CopyToAsync(memoryStream);
                var imageBytes = memoryStream.ToArray();

                // 2. Gemini Vision ile analiz et
                var visionResult = await _visionService.AnalyzeAsync(imageBytes, model.Photo.ContentType);

                if (!visionResult.IsSuccess)
                {
                    result = new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = visionResult.ErrorMessage ?? "Fotoğraf analizi başarısız.",
                        GeneratedAt = DateTime.UtcNow
                    };
                }
                else if (!visionResult.IsHuman)
                {
                    // İnsan yok → uyarı kartı
                    result = new AiResultVm
                    {
                        IsSuccess = false,
                        IsHuman = false,
                        PhotoDescription = visionResult.Description,
                        ErrorMessage = "Lütfen bir insan fotoğrafı yükleyin.",
                        GeneratedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // 3. DeepSeek ile plan üret
                    result = await _textService.GetPhotoModeRecommendationAsync(visionResult, model);

                    // 4. fal.ai ile after görsel üret (SADECE Photo Mode, opsiyonel)
                    // Başarısız olursa plan yine gösterilir, graceful fallback
                    if (result.IsSuccess && _falService.IsConfigured)
                    {
                        try
                        {
                            _logger.LogInformation("Generating after image with fal.ai for goal: {Goal}", model.Hedef);
                            var afterUrl = await _falService.GenerateAfterImageAsync(
                                imageBytes, 
                                model.Photo.ContentType, 
                                model.Hedef
                            );
                            result.AfterGeneratedImageUrl = afterUrl;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "fal.ai image generation failed, continuing with plan only");
                            // Graceful fallback - plan yine gösterilir
                        }
                    }
                }
            }
            else
            {
                // ===== DATA MODU =====
                _logger.LogInformation("Data mode request from UyeId: {UyeId}", uye.Id);
                result = await _textService.GetRecommendationAsync(model);
            }

            stopwatch.Stop();

            // ===== GÖRSEL EŞLEŞTİRME (Kural Tabanlı) =====
            // DeepSeek/Groq değişmeden, sadece lokal mapping ile before/after görseller eklenir
            if (result.IsSuccess && result.IsHuman)
            {
                var imageMapping = _imageMapper.GetTransformationImages(
                    result.BodyCategory,
                    model.Hedef,
                    model.Cinsiyet
                );
                result.BeforeImagePath = imageMapping.BeforePath;
                result.AfterImagePath = imageMapping.AfterPath;
                result.TransformationCaption = imageMapping.Caption;
            }

            // Sonucu logla
            await LogToDbAsync(model, result, uye.Id, stopwatch.ElapsedMilliseconds);

            ViewBag.Result = result;
            ViewBag.ShowResult = true;
            ViewBag.VisionEnabled = _visionService.IsConfigured;

            return View(model);
        }

        private static (bool isValid, string? error) ValidatePhoto(IFormFile photo)
        {
            if (photo.Length > MaxPhotoSize)
            {
                return (false, "Fotoğraf boyutu en fazla 5 MB olabilir.");
            }

            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(extension))
            {
                return (false, "Sadece JPG ve PNG formatları kabul edilmektedir.");
            }

            if (!AllowedPhotoContentTypes.Contains(photo.ContentType.ToLowerInvariant()))
            {
                return (false, "Geçersiz dosya formatı.");
            }

            return (true, null);
        }

        private async Task LogToDbAsync(AiRecommendVm input, AiResultVm result, int uyeId, long elapsedMs)
        {
            try
            {
                var log = new AiLog
                {
                    UyeId = uyeId,
                    SoruMetni = input.IsPhotoMode ? "Photo mode" : $"Boy:{input.Boy}, Kilo:{input.Kilo}, Hedef:{input.Hedef}",
                    CevapMetni = result.Summary ?? result.ErrorMessage ?? "N/A",
                    IsSuccess = result.IsSuccess,
                    DurationMs = (int)elapsedMs,
                    OlusturulmaZamani = DateTime.UtcNow,
                    ErrorMessage = result.ErrorMessage
                };

                _context.AiLoglar.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI request to database");
            }
        }
    }
}
