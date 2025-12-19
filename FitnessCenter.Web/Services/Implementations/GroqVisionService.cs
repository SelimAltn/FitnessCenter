using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Groq Vision servisi
    /// Fotoğraf analizi: insan tespiti + vücut sınıflandırması
    /// Llama 3.2 Vision modeli kullanır
    /// </summary>
    public class GroqVisionService : IAiVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly GroqSettings _settings;
        private readonly ILogger<GroqVisionService> _logger;

        private const int MaxRetries = 3;

        public GroqVisionService(
            HttpClient httpClient,
            IOptions<GroqSettings> settings,
            ILogger<GroqVisionService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<VisionResult> AnalyzeAsync(byte[] imageBytes, string contentType)
        {
            if (!IsConfigured)
            {
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Groq Vision servisi yapılandırılmamış."
                };
            }

            try
            {
                var apiUrl = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";
                var requestBody = BuildVisionRequest(imageBytes, contentType);
                var jsonContent = JsonSerializer.Serialize(requestBody);

                _logger.LogInformation("Calling Groq Vision API with model: {Model}", _settings.VisionModel);

                int attempt = 0;
                int delayMs = 1000;

                while (attempt < MaxRetries)
                {
                    attempt++;

                    try
                    {
                        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
                        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                        var response = await _httpClient.SendAsync(request);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return ParseVisionResponse(responseBody);
                        }

                        var statusCode = (int)response.StatusCode;

                        if ((statusCode == 429 || statusCode == 503) && attempt < MaxRetries)
                        {
                            _logger.LogWarning("Groq Vision API {StatusCode}, retrying in {Delay}ms", statusCode, delayMs);
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                            continue;
                        }

                        _logger.LogError("Groq Vision API error: {StatusCode} - {Body}", statusCode, responseBody);
                        return new VisionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = GetErrorMessage(statusCode, responseBody)
                        };
                    }
                    catch (TaskCanceledException)
                    {
                        if (attempt < MaxRetries)
                        {
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                            continue;
                        }
                        return new VisionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = "Fotoğraf analizi zaman aşımına uğradı."
                        };
                    }
                }

                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Maksimum deneme sayısına ulaşıldı."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GroqVisionService.AnalyzeAsync");
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi sırasında hata oluştu."
                };
            }
        }

        private object BuildVisionRequest(byte[] imageBytes, string contentType)
        {
            var base64Image = Convert.ToBase64String(imageBytes);

            var prompt = @"Bu görselde insan var mı? 
Varsa vücut tipini yalnızca şunlardan biri olarak sınıflandır: Zayif, Sisman, Kasli, Normal.
1 cümle kısa açıklama yaz.
İnsan yoksa ne olduğunu 1 cümle yaz ve isHuman=false döndür.

SADECE şu JSON formatında yanıt ver, başka hiçbir şey yazma:
{""isHuman"": true, ""bodyCategory"": ""Normal"", ""description"": ""Açıklama""}";

            return new
            {
                model = _settings.VisionModel,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new
                            {
                                type = "image_url",
                                image_url = new
                                {
                                    url = $"data:{contentType};base64,{base64Image}"
                                }
                            },
                            new
                            {
                                type = "text",
                                text = prompt
                            }
                        }
                    }
                },
                temperature = 0.1,
                max_tokens = 200
            };
        }

        private VisionResult ParseVisionResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var content))
                    {
                        var textContent = content.GetString() ?? "";
                        var jsonText = ExtractJson(textContent);
                        return ParseJsonResult(jsonText);
                    }
                }

                _logger.LogWarning("Could not parse Groq Vision response: {Response}", responseJson);
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi yanıtı beklenmeyen formatta."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Groq Vision response");
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi yanıtı işlenirken hata oluştu."
                };
            }
        }

        private static string ExtractJson(string content)
        {
            content = content.Trim();

            if (content.StartsWith("```json"))
                content = content[7..];
            else if (content.StartsWith("```"))
                content = content[3..];

            if (content.EndsWith("```"))
                content = content[..^3];

            content = content.Trim();

            if (!content.StartsWith("{"))
            {
                var startIndex = content.IndexOf('{');
                var endIndex = content.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    content = content.Substring(startIndex, endIndex - startIndex + 1);
                }
            }

            return content;
        }

        private VisionResult ParseJsonResult(string jsonContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                return new VisionResult
                {
                    IsSuccess = true,
                    IsHuman = root.TryGetProperty("isHuman", out var ih) && ih.GetBoolean(),
                    BodyCategory = root.TryGetProperty("bodyCategory", out var bc) ? bc.GetString() ?? "Belirsiz" : "Belirsiz",
                    Description = root.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""
                };
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Could not parse vision JSON result: {Content}", jsonContent);
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi sonucu okunamadı."
                };
            }
        }

        private static string GetErrorMessage(int statusCode, string responseBody)
        {
            return statusCode switch
            {
                400 => "Geçersiz fotoğraf formatı.",
                401 => "Groq API anahtarı geçersiz.",
                404 => "Vision modeli bulunamadı.",
                429 => "Çok fazla istek. Lütfen bekleyin.",
                _ => $"Fotoğraf analizi hatası (HTTP {statusCode})."
            };
        }
    }
}
