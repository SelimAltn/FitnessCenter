using FitnessCenter.Web.Models;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Stability AI SDXL Image-to-Image servisi
    /// Kullanıcı fotoğrafından hedefe göre "after" görsel üretir
    /// SADECE Photo Mode'da kullanılır
    /// </summary>
    public class StabilityImageToImageService
    {
        private readonly HttpClient _httpClient;
        private readonly StabilitySettings _settings;
        private readonly ILogger<StabilityImageToImageService> _logger;

        // Hedef bazlı prompt şablonları - 6 AYLIK PLAN İÇİN AGRESİF DÖNÜŞÜM
        // Çok belirgin değişim - zayıflama/kas kazanma görünür olmalı
        private static readonly Dictionary<string, PromptConfig> GoalConfigs = new()
        {
            ["lean"] = new PromptConfig
            {
                // MAKSİMUM DÖNÜŞÜM - yüz değişebilir ama kilo kaybı çok belirgin olacak
                Prompt = "thin slim athletic person, very lean body, flat stomach, narrow waist, " +
                         "visible weight loss transformation result, slim figure, no belly fat, " +
                         "fit healthy person after diet, dramatic body transformation, " +
                         "high quality photo, natural lighting",
                ImageStrength = 0.99f,  // MAKSIMUM - neredeyse tamamen yeni görsel
                CfgScale = 12,          // Prompt'a çok sadık
                Steps = 40              // Maximum detay
            },
            ["muscle"] = new PromptConfig
            {
                // 6 aylık kas kazanma = belirgin kas artışı
                Prompt = "dramatic fitness transformation, significant muscle gain after 6 months, " +
                         "much more muscular body, visibly broader shoulders, defined chest muscles, " +
                         "athletic muscular physique, major body transformation result, " +
                         "before and after muscle building success, toned arms, " +
                         "same person same face, natural lighting, high quality photo",
                ImageStrength = 0.85f,
                CfgScale = 10,
                Steps = 30
            },
            ["fit"] = new PromptConfig
            {
                // 6 aylık fit kalma = dengeli dönüşüm
                Prompt = "fitness transformation, fit and toned athletic body, " +
                         "balanced proportions, healthy athletic look, defined muscles, " +
                         "same person same face, natural lighting, high quality photo",
                ImageStrength = 0.75f,
                CfgScale = 8,
                Steps = 25
            }
        };

        // Negative prompt - değişim olmamasını ve yaş değişimini engelle
        private const string NegativePrompt = 
            "no change, identical, same image, unchanged body, before photo, same weight, still fat, still overweight, " +
            "younger, baby face, face change, different face, beauty retouch, plastic skin, " +
            "blurry, low quality, deformed, extra limbs, cartoon, unrealistic, " +
            "bodybuilder, extreme muscles, exaggerated anatomy, different person";

        public StabilityImageToImageService(
            HttpClient httpClient,
            IOptions<StabilitySettings> settings,
            ILogger<StabilityImageToImageService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        /// <summary>
        /// API Key yapılandırılmış mı?
        /// </summary>
        public bool IsConfigured => _settings.IsConfigured;

        /// <summary>
        /// Kullanıcı fotoğrafından hedefe göre after görsel üretir
        /// </summary>
        /// <param name="imageBytes">Kullanıcının yüklediği fotoğraf</param>
        /// <param name="contentType">Fotoğraf content type (image/jpeg, image/png)</param>
        /// <param name="goal">Hedef (Kilo Verme, Kas Kazanma, Fit Kalma)</param>
        /// <returns>Üretilen görsel URL'si (base64 data URI) veya null (başarısız/filtered)</returns>
        public async Task<string?> GenerateAfterImageAsync(byte[] imageBytes, string contentType, string? goal)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("StabilityImageToImageService: API key not configured, skipping image generation");
                return null;
            }

            try
            {
                // 1. Hedef bazlı config al
                var goalType = GetGoalType(goal);
                var config = GoalConfigs[goalType];
                _logger.LogInformation("[Step 1] Goal type: {Goal}", goalType);

                // Debug: Input image hash
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var inputHash = Convert.ToHexString(sha256.ComputeHash(imageBytes))[..16];
                
                _logger.LogInformation(
                    "[Step 2] Stability AI call - Goal: {Goal}, ImageStrength: {Strength}, CfgScale: {Cfg}, Steps: {Steps}, InputHash: {Hash}, InputSize: {Size}KB",
                    goalType, config.ImageStrength, config.CfgScale, config.Steps, inputHash, imageBytes.Length / 1024);

                // 2. Gövdeye odaklanmak için görüntüyü crop et
                _logger.LogInformation("[Step 3] Starting crop...");
                var (croppedBytes, croppedContentType) = CropToTorso(imageBytes, contentType);
                _logger.LogInformation("[Step 3] Cropped image: {OriginalSize}KB → {CroppedSize}KB", 
                    imageBytes.Length / 1024, croppedBytes.Length / 1024);

                // 3. Stability AI API çağrısı (cropped image ile)
                _logger.LogInformation("[Step 4] Calling Stability AI API...");
                var result = await CallStabilityApiAsync(croppedBytes, croppedContentType, config);
                
                if (result == null)
                {
                    _logger.LogWarning("[Step 5] API returned null - check API logs above for details");
                }
                else
                {
                    _logger.LogInformation("[Step 5] API returned image successfully");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FATAL: Error generating after image with Stability AI - Message: {Message}, StackTrace: {Stack}", 
                    ex.Message, ex.StackTrace);
                return null;
            }
        }

        private string GetGoalType(string? goal)
        {
            if (string.IsNullOrEmpty(goal))
                return "fit";

            var lower = goal.ToLowerInvariant();

            if (lower.Contains("kilo") || lower.Contains("zayıf") || lower.Contains("lean"))
                return "lean";

            if (lower.Contains("kas") || lower.Contains("muscle"))
                return "muscle";

            return "fit";
        }

        private async Task<string?> CallStabilityApiAsync(byte[] imageBytes, string contentType, PromptConfig config)
        {
            // Stability AI image-to-image endpoint
            var endpoint = $"{_settings.BaseUrl}/v1/generation/{_settings.Model}/image-to-image";

            _logger.LogInformation("Calling Stability AI API - Prompt: {Prompt}", config.Prompt[..Math.Min(100, config.Prompt.Length)] + "...");

            // Multipart form data oluştur
            using var formContent = new MultipartFormDataContent();

            // Init image - Content-Type mutlaka set et
            using var imageStream = new MemoryStream(imageBytes);
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            formContent.Add(imageContent, "init_image", contentType.Contains("png") ? "image.png" : "image.jpg");

            // Text prompts - positive (weight = 1)
            formContent.Add(new StringContent(config.Prompt), "text_prompts[0][text]");
            formContent.Add(new StringContent("1"), "text_prompts[0][weight]");

            // Text prompts - negative (weight = -1)
            formContent.Add(new StringContent(NegativePrompt), "text_prompts[1][text]");
            formContent.Add(new StringContent("-1"), "text_prompts[1][weight]");

            // Hedef bazlı parametreler
            formContent.Add(new StringContent(config.ImageStrength.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)), "image_strength");
            formContent.Add(new StringContent(config.CfgScale.ToString()), "cfg_scale");
            formContent.Add(new StringContent(config.Steps.ToString()), "steps");
            formContent.Add(new StringContent("K_EULER_ANCESTRAL"), "sampler");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = formContent;

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Stability AI API error: {StatusCode} - {Body}",
                    (int)response.StatusCode, responseBody);
                return null;
            }

            // Response: { "artifacts": [{ "base64": "...", "finishReason": "SUCCESS" }] }
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("artifacts", out var artifacts) &&
                    artifacts.GetArrayLength() > 0)
                {
                    var firstArtifact = artifacts[0];

                    // finishReason kontrolü
                    if (firstArtifact.TryGetProperty("finishReason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        _logger.LogInformation("Stability AI finishReason: {Reason}", reason);

                        // CONTENT_FILTERED = safety blur → null döndür
                        if (reason == "CONTENT_FILTERED")
                        {
                            _logger.LogWarning("Stability AI: Content was filtered by safety checker - returning null");
                            return null; // UI'da uyarı gösterilecek
                        }
                    }

                    if (firstArtifact.TryGetProperty("base64", out var base64))
                    {
                        var base64String = base64.GetString();
                        if (!string.IsNullOrEmpty(base64String))
                        {
                            // Output debug logging - gerçekten farklı görsel mi?
                            var outputHead = base64String[..Math.Min(16, base64String.Length)];
                            var outputBytes = Convert.FromBase64String(base64String);
                            using var outSha256 = System.Security.Cryptography.SHA256.Create();
                            var outputHash = Convert.ToHexString(outSha256.ComputeHash(outputBytes))[..16];
                            
                            _logger.LogInformation(
                                "Stability AI output - Length: {Len}, Head: {Head}..., Hash: {Hash}, Size: {Size}KB",
                                base64String.Length, outputHead, outputHash, outputBytes.Length / 1024);
                            
                            return $"data:image/png;base64,{base64String}";
                        }
                    }
                }

                _logger.LogWarning("Stability AI response missing artifacts array");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Stability AI response");
                return null;
            }
        }

        /// <summary>
        /// Hedef bazlı prompt ve parametre konfigürasyonu
        /// </summary>
        private class PromptConfig
        {
            public string Prompt { get; set; } = "";
            public float ImageStrength { get; set; } = 0.65f;
            public int CfgScale { get; set; } = 7;
            public int Steps { get; set; } = 25;
        }

        /// <summary>
        /// Görüntüyü gövdeye odaklanacak şekilde crop eder ve SDXL için 1024x1024'e resize eder
        /// </summary>
        private (byte[] croppedBytes, string contentType) CropToTorso(byte[] imageBytes, string contentType)
        {
            // SDXL geçerli boyutlar: 1024x1024, 1152x896, vs. - 1024x1024 en güvenli
            const int SDXL_SIZE = 1024;
            
            try
            {
                using var inputStream = new MemoryStream(imageBytes);
                using var originalBitmap = SKBitmap.Decode(inputStream);
                
                if (originalBitmap == null)
                {
                    _logger.LogWarning("Failed to decode image for cropping, using original");
                    return (imageBytes, contentType);
                }

                int originalWidth = originalBitmap.Width;
                int originalHeight = originalBitmap.Height;

                // Crop oranları - gövdeye odaklan
                float topCropRatio = 0.15f;    // Üstten %15 (baş azalt)
                float bottomCropRatio = 0.10f; // Alttan %10
                float sideCropRatio = 0.10f;   // Yanlardan %10

                int cropTop = (int)(originalHeight * topCropRatio);
                int cropBottom = (int)(originalHeight * bottomCropRatio);
                int cropSide = (int)(originalWidth * sideCropRatio);

                int cropWidth = originalWidth - (2 * cropSide);
                int cropHeight = originalHeight - cropTop - cropBottom;

                // Crop rect
                var cropRect = new SKRectI(cropSide, cropTop, cropSide + cropWidth, cropTop + cropHeight);

                // Crop + SDXL boyutuna resize (1024x1024)
                using var finalBitmap = new SKBitmap(SDXL_SIZE, SDXL_SIZE);
                using var canvas = new SKCanvas(finalBitmap);
                
                // High quality resize
                using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
                canvas.DrawBitmap(originalBitmap, cropRect, new SKRect(0, 0, SDXL_SIZE, SDXL_SIZE), paint);

                // JPEG olarak encode et
                using var outputStream = new MemoryStream();
                using var image = SKImage.FromBitmap(finalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
                data.SaveTo(outputStream);

                _logger.LogInformation(
                    "Cropped: {OW}x{OH} → crop({CW}x{CH}) → resize(1024x1024)",
                    originalWidth, originalHeight, cropWidth, cropHeight);

                return (outputStream.ToArray(), "image/jpeg");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Crop failed, using original image");
                return (imageBytes, contentType);
            }
        }
    }
}
