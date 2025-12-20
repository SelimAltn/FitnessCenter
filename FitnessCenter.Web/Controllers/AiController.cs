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
        private readonly OpenAIImageService _openAIImageService;

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
            OpenAIImageService openAIImageService)
        {
            _textService = textService;
            _visionService = visionService;
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _imageMapper = imageMapper;
            _openAIImageService = openAIImageService;
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
        /// GET: /Ai/Detail/{id} - Geçmiş AI önerisinin detayını gösterir
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var uye = await GetCurrentMemberAsync();
            if (uye == null)
            {
                TempData["Error"] = "Sistemde size bağlı bir üye kaydı bulunamadı.";
                return RedirectToAction("UyeOl", "Uyelik");
            }

            // Sadece kendi kaydına erişebilsin
            var aiLog = await _context.AiLoglar
                .FirstOrDefaultAsync(a => a.Id == id && a.UyeId == uye.Id);

            if (aiLog == null)
            {
                TempData["Error"] = "AI öneri kaydı bulunamadı.";
                return RedirectToAction(nameof(History));
            }

            // ResponseJson'dan AiResultVm parse et
            AiResultVm? result = null;
            if (!string.IsNullOrEmpty(aiLog.ResponseJson))
            {
                try
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<AiResultVm>(aiLog.ResponseJson, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse ResponseJson for AiLog {Id}", id);
                }
            }

            // Fallback: ResponseJson yoksa veya parse edilemezse basit bir sonuç oluştur
            if (result == null)
            {
                result = new AiResultVm
                {
                    IsSuccess = aiLog.IsSuccess,
                    Summary = aiLog.CevapMetni,
                    ErrorMessage = aiLog.ErrorMessage,
                    GeneratedAt = aiLog.OlusturulmaZamani
                };
            }

            ViewBag.AiLog = aiLog;
            ViewBag.IsPhotoMode = aiLog.SoruMetni?.StartsWith("Photo") ?? false;
            
            return View(result);
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

                // 1a. Kullanıcının yüklediği fotoğrafı diske kaydet (Before image için)
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ai-photos");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(model.Photo.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);
                var uploadedPhotoUrl = $"/uploads/ai-photos/{fileName}";

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

                    // 4. OpenAI ile after görsel üret (IMAGE-TO-IMAGE, referans foto ile)
                    // Başarısız olursa plan yine gösterilir, graceful fallback
                    if (result.IsSuccess && _openAIImageService.IsConfigured)
                    {
                        try
                        {
                            // Cinsiyet çıkarımı (Vision description'dan veya heuristik)
                            var gender = ExtractGenderFromDescription(visionResult.Description);
                            
                            _logger.LogInformation("Generating after image with OpenAI for goal: {Goal}, gender: {Gender}", 
                                model.Hedef, gender ?? "unknown");
                            
                            var afterUrl = await _openAIImageService.GenerateAfterImageAsync(
                                imageBytes, 
                                model.Photo.ContentType, 
                                model.Hedef,
                                gender
                            );
                            result.AfterGeneratedImageUrl = afterUrl;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "OpenAI image generation failed, continuing with plan only");
                            // Graceful fallback - plan yine gösterilir
                        }
                    }

                    // Photo mode'da kullanıcının yüklediği fotoğrafı BeforeImagePath olarak ata
                    result.BeforeImagePath = uploadedPhotoUrl;
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
            // Photo modunda BeforeImagePath zaten kullanıcının fotoğrafına ayarlandı, üzerine yazma
            if (result.IsSuccess && result.IsHuman)
            {
                var imageMapping = _imageMapper.GetTransformationImages(
                    result.BodyCategory,
                    model.Hedef,
                    model.Cinsiyet
                );
                
                // Sadece data modunda (photo değilse) BeforeImagePath ata
                if (!model.IsPhotoMode)
                {
                    result.BeforeImagePath = imageMapping.BeforePath;
                }
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
                // Serialize the full result for later retrieval
                string? responseJson = null;
                try
                {
                    responseJson = System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
                    {
                        PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });
                }
                catch
                {
                    // Ignore serialization errors
                }

                var log = new AiLog
                {
                    UyeId = uyeId,
                    SoruMetni = input.IsPhotoMode ? "Photo mode" : $"Boy:{input.Boy}, Kilo:{input.Kilo}, Hedef:{input.Hedef}",
                    CevapMetni = result.Summary ?? result.ErrorMessage ?? "N/A",
                    IsSuccess = result.IsSuccess,
                    DurationMs = (int)elapsedMs,
                    OlusturulmaZamani = DateTime.UtcNow,
                    ErrorMessage = result.ErrorMessage,
                    ResponseJson = responseJson
                };

                _context.AiLoglar.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log AI request to database");
            }
        }

        /// <summary>
        /// Vision description'dan cinsiyet çıkarımı yapar
        /// OpenAI'ye gönderilen prompt'ta cinsiyet korunması için kullanılır
        /// </summary>
        private static string? ExtractGenderFromDescription(string? description)
        {
            if (string.IsNullOrEmpty(description)) return null;

            var lower = description.ToLowerInvariant();

            // Erkek göstergeleri
            if (lower.Contains("male") || lower.Contains("man") || lower.Contains("erkek") ||
                lower.Contains("boy") || lower.Contains("guy") || lower.Contains("gentleman"))
            {
                // "female" içeriyorsa kadın, değilse erkek
                if (!lower.Contains("female") && !lower.Contains("woman") && !lower.Contains("kadın"))
                {
                    return "male";
                }
            }

            // Kadın göstergeleri
            if (lower.Contains("female") || lower.Contains("woman") || lower.Contains("kadın") ||
                lower.Contains("girl") || lower.Contains("lady"))
            {
                return "female";
            }

            return null;
        }
    }
}
