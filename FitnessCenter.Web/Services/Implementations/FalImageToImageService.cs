using FitnessCenter.Web.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// fal.ai FLUX Image-to-Image servisi - AGRESİF VÜCUT DÖNÜŞÜMÜ
    /// Yüz farklı olabilir ama vücut belirgin şekilde değişecek
    /// </summary>
    public class FalImageToImageService
    {
        private readonly HttpClient _httpClient;
        private readonly FalSettings _settings;
        private readonly ILogger<FalImageToImageService> _logger;

        // Hedef bazlı prompt - SADECE VÜCUT ODAKLI
        private static readonly Dictionary<string, PromptConfig> GoalConfigs = new()
        {
            ["lean"] = new PromptConfig
            {
                Prompt = "extremely thin slim athletic person, very lean body, flat stomach, narrow waist, " +
                         "no belly fat, skinny torso, fit healthy body after major weight loss, " +
                         "professional fitness photo, high quality, realistic",
                Strength = 0.95
            },
            ["muscle"] = new PromptConfig
            {
                Prompt = "very muscular athletic person, bodybuilder physique, big shoulders, defined chest, " +
                         "visible biceps, athletic muscular body, fitness model, " +
                         "professional fitness photo, high quality, realistic",
                Strength = 0.90
            },
            ["fit"] = new PromptConfig
            {
                Prompt = "fit toned athletic person, healthy body, balanced proportions, " +
                         "visible muscle tone, athletic physique, " +
                         "professional fitness photo, high quality, realistic",
                Strength = 0.85
            }
        };

        private const string NegativePrompt = "fat, overweight, obese, big belly, same as before, unchanged, blurry, low quality, deformed";

        public FalImageToImageService(
            HttpClient httpClient,
            IOptions<FalSettings> settings,
            ILogger<FalImageToImageService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<string?> GenerateAfterImageAsync(byte[] imageBytes, string contentType, string? goal)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("FalImageToImageService: API key not configured");
                return null;
            }

            try
            {
                var goalType = GetGoalType(goal);
                var config = GoalConfigs[goalType];

                _logger.LogInformation("[fal.ai] Goal: {Goal}, Strength: {Strength}", goalType, config.Strength);

                // Base64 data URL
                var base64Image = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{contentType};base64,{base64Image}";

                var result = await CallFalApiAsync(dataUrl, config);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image with fal.ai");
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

        private async Task<string?> CallFalApiAsync(string imageDataUrl, PromptConfig config)
        {
            // fal.ai FLUX img2img endpoint
            var endpoint = "https://fal.run/fal-ai/flux/dev/image-to-image";

            var requestBody = new
            {
                image_url = imageDataUrl,
                prompt = config.Prompt,
                negative_prompt = NegativePrompt,
                strength = config.Strength,
                num_inference_steps = 28,
                guidance_scale = 7.5
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("[fal.ai] Calling API...");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Key", _settings.ApiKey);

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[fal.ai] API error: {Status} - {Body}", (int)response.StatusCode, responseBody);
                return null;
            }

            // Parse response
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("images", out var images) && 
                    images.GetArrayLength() > 0)
                {
                    var firstImage = images[0];
                    if (firstImage.TryGetProperty("url", out var urlProp))
                    {
                        var imageUrl = urlProp.GetString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            _logger.LogInformation("[fal.ai] Image generated successfully");
                            // URL'den base64'e çevir
                            return await DownloadAsBase64Async(imageUrl);
                        }
                    }
                }

                // Alternatif format
                if (root.TryGetProperty("image", out var imageObj))
                {
                    if (imageObj.TryGetProperty("url", out var urlProp2))
                    {
                        var imageUrl = urlProp2.GetString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            _logger.LogInformation("[fal.ai] Image generated successfully (alt format)");
                            return await DownloadAsBase64Async(imageUrl);
                        }
                    }
                }

                _logger.LogWarning("[fal.ai] No image in response: {Body}", responseBody[..Math.Min(500, responseBody.Length)]);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[fal.ai] Failed to parse response");
                return null;
            }
        }

        private async Task<string?> DownloadAsBase64Async(string imageUrl)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                var base64 = Convert.ToBase64String(imageBytes);
                _logger.LogInformation("[fal.ai] Downloaded image: {Size}KB", imageBytes.Length / 1024);
                return $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[fal.ai] Failed to download image");
                return null;
            }
        }

        private class PromptConfig
        {
            public string Prompt { get; set; } = "";
            public double Strength { get; set; } = 0.9;
        }
    }
}
