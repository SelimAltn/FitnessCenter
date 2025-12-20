using FitnessCenter.Web.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Replicate API servisi - vücut dönüşümü için img2img
    /// Yüz değişebilir ama vücut belirgin şekilde değişecek
    /// </summary>
    public class ReplicateImageService
    {
        private readonly HttpClient _httpClient;
        private readonly ReplicateSettings _settings;
        private readonly ILogger<ReplicateImageService> _logger;

        // Hedef bazlı prompt - SADECE VÜCUT ODAKLI (yüz önemli değil)
        private static readonly Dictionary<string, PromptConfig> GoalConfigs = new()
        {
            ["lean"] = new PromptConfig
            {
                Prompt = "very thin slim athletic person, extremely lean body, flat stomach, narrow waist, " +
                         "no belly fat at all, skinny torso, visible abs, fit healthy body after weight loss, " +
                         "professional fitness photo, high quality, realistic",
                NegativePrompt = "fat, overweight, obese, chubby, belly, big stomach, same as before",
                Strength = 0.95  // Çok yüksek - vücut tamamen değişecek
            },
            ["muscle"] = new PromptConfig
            {
                Prompt = "very muscular athletic person, bodybuilder physique, big shoulders, defined chest muscles, " +
                         "visible biceps and triceps, athletic muscular body, fitness model, " +
                         "professional fitness photo, high quality, realistic",
                NegativePrompt = "thin, skinny, weak, no muscles, same as before",
                Strength = 0.90
            },
            ["fit"] = new PromptConfig
            {
                Prompt = "fit toned athletic person, healthy body, balanced proportions, " +
                         "visible muscle tone, athletic physique, " +
                         "professional fitness photo, high quality, realistic",
                NegativePrompt = "fat, overweight, same as before",
                Strength = 0.85
            }
        };

        public ReplicateImageService(
            HttpClient httpClient,
            IOptions<ReplicateSettings> settings,
            ILogger<ReplicateImageService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _settings.ApiToken);
        }

        public bool IsConfigured => _settings.IsConfigured;

        /// <summary>
        /// Vücut dönüşümü görseli üretir - yüz değişebilir, vücut belirgin değişecek
        /// </summary>
        public async Task<string?> GenerateAfterImageAsync(byte[] imageBytes, string contentType, string? goal)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("ReplicateImageService: API token not configured");
                return null;
            }

            try
            {
                var goalType = GetGoalType(goal);
                var config = GoalConfigs[goalType];

                _logger.LogInformation("[Replicate] Goal: {Goal}, Strength: {Strength}", goalType, config.Strength);

                // Base64 data URL oluştur
                var base64Image = Convert.ToBase64String(imageBytes);
                var dataUrl = $"data:{contentType};base64,{base64Image}";

                // Replicate API çağrısı
                var result = await CallReplicateApiAsync(dataUrl, config);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating image with Replicate");
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

        private async Task<string?> CallReplicateApiAsync(string imageDataUrl, PromptConfig config)
        {
            var endpoint = $"{_settings.BaseUrl}/predictions";

            // Request body
            var requestBody = new
            {
                version = _settings.ModelVersion.Split(':').LastOrDefault() ?? _settings.ModelVersion,
                input = new
                {
                    image = imageDataUrl,
                    prompt = config.Prompt,
                    negative_prompt = config.NegativePrompt,
                    prompt_strength = config.Strength,
                    num_inference_steps = 30,
                    guidance_scale = 10.0,
                    scheduler = "K_EULER"
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            _logger.LogInformation("[Replicate] Calling API with prompt: {Prompt}", config.Prompt[..Math.Min(80, config.Prompt.Length)] + "...");

            var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Prefer", "wait"); // Senkron bekle

            var response = await _httpClient.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("[Replicate] API error: {Status} - {Body}", (int)response.StatusCode, responseBody);
                return null;
            }

            // Response parse
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // Status kontrolü
                if (root.TryGetProperty("status", out var status))
                {
                    var statusStr = status.GetString();
                    _logger.LogInformation("[Replicate] Status: {Status}", statusStr);

                    if (statusStr == "failed")
                    {
                        if (root.TryGetProperty("error", out var error))
                        {
                            _logger.LogError("[Replicate] Failed: {Error}", error.GetString());
                        }
                        return null;
                    }

                    // Henüz bitmemişse polling yap
                    if (statusStr == "starting" || statusStr == "processing")
                    {
                        if (root.TryGetProperty("id", out var idProp))
                        {
                            var predictionId = idProp.GetString();
                            return await PollForResultAsync(predictionId!);
                        }
                    }
                }

                // Output al
                if (root.TryGetProperty("output", out var output))
                {
                    if (output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
                    {
                        var imageUrl = output[0].GetString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            _logger.LogInformation("[Replicate] Image generated successfully");
                            // URL'den image'ı indir ve base64'e çevir
                            return await DownloadAsBase64Async(imageUrl);
                        }
                    }
                    else if (output.ValueKind == JsonValueKind.String)
                    {
                        var imageUrl = output.GetString();
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            return await DownloadAsBase64Async(imageUrl);
                        }
                    }
                }

                _logger.LogWarning("[Replicate] No output in response");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Replicate] Failed to parse response");
                return null;
            }
        }

        private async Task<string?> PollForResultAsync(string predictionId)
        {
            var pollEndpoint = $"{_settings.BaseUrl}/predictions/{predictionId}";
            var maxAttempts = 30;
            var delayMs = 2000;

            for (int i = 0; i < maxAttempts; i++)
            {
                await Task.Delay(delayMs);

                var response = await _httpClient.GetAsync(pollEndpoint);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode) continue;

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var status))
                {
                    var statusStr = status.GetString();
                    _logger.LogDebug("[Replicate] Poll {Attempt}: {Status}", i + 1, statusStr);

                    if (statusStr == "succeeded")
                    {
                        if (root.TryGetProperty("output", out var output))
                        {
                            if (output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
                            {
                                var imageUrl = output[0].GetString();
                                if (!string.IsNullOrEmpty(imageUrl))
                                {
                                    _logger.LogInformation("[Replicate] Image ready after {Attempts} polls", i + 1);
                                    return await DownloadAsBase64Async(imageUrl);
                                }
                            }
                        }
                    }
                    else if (statusStr == "failed")
                    {
                        _logger.LogError("[Replicate] Prediction failed");
                        return null;
                    }
                }
            }

            _logger.LogWarning("[Replicate] Polling timeout");
            return null;
        }

        private async Task<string?> DownloadAsBase64Async(string imageUrl)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(imageUrl);
                var base64 = Convert.ToBase64String(imageBytes);
                _logger.LogInformation("[Replicate] Downloaded image: {Size}KB", imageBytes.Length / 1024);
                return $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Replicate] Failed to download image");
                return null;
            }
        }

        private class PromptConfig
        {
            public string Prompt { get; set; } = "";
            public string NegativePrompt { get; set; } = "";
            public double Strength { get; set; } = 0.9;
        }
    }
}
