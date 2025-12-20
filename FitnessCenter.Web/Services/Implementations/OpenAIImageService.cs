using FitnessCenter.Web.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// OpenAI Image API servisi - IMAGE-TO-IMAGE editing
    /// Kullanıcının fotoğrafını referans alarak dönüşüm görseli üretir
    /// gpt-image-1 modeli ile /images/edits endpoint kullanır
    /// </summary>
    public class OpenAIImageService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenAIImageSettings _settings;
        private readonly ILogger<OpenAIImageService> _logger;

        public OpenAIImageService(
            HttpClient httpClient,
            IOptions<OpenAIImageSettings> settings,
            ILogger<OpenAIImageService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        }

        public bool IsConfigured => _settings.IsConfigured;

        /// <summary>
        /// IMAGE-TO-IMAGE: Kullanıcının fotoğrafını referans alarak dönüşüm görseli üretir
        /// Aynı kişi, aynı cinsiyet, aynı kıyafet/ortam korunur, sadece vücut dönüşümü yapılır
        /// </summary>
        public async Task<string?> GenerateAfterImageAsync(byte[] imageBytes, string contentType, string? goal, string? gender = null)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("[OpenAI Image] API key not configured");
                return null;
            }

            try
            {
                var goalType = GetGoalType(goal);
                var prompt = BuildImageEditPrompt(goalType, gender);

                _logger.LogInformation("[OpenAI Image] Goal: {Goal}, Gender: {Gender}, Model: {Model}", 
                    goalType, gender ?? "unknown", _settings.Model);

                // Image resize if needed (max 4MB for best results)
                var processedImage = ProcessImageForApi(imageBytes, contentType);

                // OpenAI Image Edit API çağrısı (multipart form)
                var result = await CallOpenAIImageEditApiAsync(processedImage, prompt);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI Image] Error generating image");
                return null;
            }
        }

        private string GetGoalType(string? goal)
        {
            if (string.IsNullOrEmpty(goal)) return "fit";
            var lower = goal.ToLowerInvariant();
            if (lower.Contains("kilo") || lower.Contains("zayıf") || lower.Contains("lean")) return "lean";
            if (lower.Contains("kas") || lower.Contains("muscle")) return "muscle";
            return "fit";
        }

        /// <summary>
        /// SAFE PROMPT - OpenAI content moderation için uygun
        /// Professional fitness context, appropriate clothing vurgulu
        /// </summary>
        private string BuildImageEditPrompt(string goalType, string? gender)
        {
            var genderStr = gender?.ToLowerInvariant() switch
            {
                "male" or "erkek" => "male person",
                "female" or "kadın" or "kadin" => "female person",
                _ => "person"
            };

            var transformationDesc = goalType switch
            {
                "lean" => "healthier body with improved posture and leaner physique after fitness training",
                "muscle" => "stronger athletic build with improved muscle tone from regular exercise",
                "fit" => "healthier and more fit appearance with balanced proportions",
                _ => "healthier and more fit appearance"
            };

            // Content-safe professional fitness prompt
            return $@"Professional fitness progress photo. Show the same {genderStr} with {transformationDesc}. 

This is a health and wellness transformation showing realistic fitness progress.

Keep the same person's face and identity.
Keep appropriate athletic sportswear.
Keep the same background setting.
Keep the same pose and camera angle.
Keep professional fitness photography style.

This is a safe-for-work professional fitness transformation image.";
        }

        private byte[] ProcessImageForApi(byte[] imageBytes, string contentType)
        {
            // OpenAI accepts PNG, WebP, JPG up to 50MB
            // For best results keep under 4MB
            if (imageBytes.Length > 4 * 1024 * 1024)
            {
                _logger.LogWarning("[OpenAI Image] Image is large ({Size}KB), may need optimization", 
                    imageBytes.Length / 1024);
            }
            
            return imageBytes;
        }

        private async Task<string?> CallOpenAIImageEditApiAsync(byte[] imageBytes, string prompt)
        {
            var endpoint = $"{_settings.BaseUrl}/images/edits";

            using var formData = new MultipartFormDataContent();

            // Image file
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            formData.Add(imageContent, "image", "input.png");

            // Model
            formData.Add(new StringContent(_settings.Model), "model");

            // Prompt
            formData.Add(new StringContent(prompt), "prompt");

            // Size
            formData.Add(new StringContent(_settings.Size), "size");

            // Number of images
            formData.Add(new StringContent("1"), "n");

            _logger.LogInformation("[OpenAI Image] Calling /images/edits API with {Size}KB image...", 
                imageBytes.Length / 1024);

            var response = await _httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = formData
            });

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[OpenAI Image] API error: {Status} - {Body}", 
                    (int)response.StatusCode, responseBody[..Math.Min(500, responseBody.Length)]);
                return null;
            }

            // Parse response
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("data", out var data) && data.GetArrayLength() > 0)
                {
                    var firstImage = data[0];

                    // URL format
                    if (firstImage.TryGetProperty("url", out var urlProp))
                    {
                        var imageUrl = urlProp.GetString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            _logger.LogInformation("[OpenAI Image] Got URL, downloading...");
                            return await DownloadAsBase64Async(imageUrl);
                        }
                    }

                    // b64_json format (fallback)
                    if (firstImage.TryGetProperty("b64_json", out var b64Json))
                    {
                        var base64 = b64Json.GetString();
                        if (!string.IsNullOrEmpty(base64))
                        {
                            _logger.LogInformation("[OpenAI Image] Generated successfully (base64), size: {Size}KB", 
                                base64.Length / 1024);
                            return $"data:image/png;base64,{base64}";
                        }
                    }
                }

                _logger.LogWarning("[OpenAI Image] No image in response: {Body}", 
                    responseBody[..Math.Min(200, responseBody.Length)]);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI Image] Failed to parse response");
                return null;
            }
        }

        private async Task<string?> DownloadAsBase64Async(string imageUrl)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                var base64 = Convert.ToBase64String(imageBytes);
                _logger.LogInformation("[OpenAI Image] Downloaded: {Size}KB", imageBytes.Length / 1024);
                return $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OpenAI Image] Download failed");
                return null;
            }
        }
    }
}
