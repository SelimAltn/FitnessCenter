using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Gemini Vision servisi
    /// Fotoğraf analizi: insan tespiti + vücut sınıflandırması
    /// </summary>
    public class GeminiVisionService : IAiVisionService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly ILogger<GeminiVisionService> _logger;

        private const int MaxRetries = 3;

        public GeminiVisionService(
            HttpClient httpClient,
            IOptions<GeminiSettings> settings,
            ILogger<GeminiVisionService> logger)
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
                    ErrorMessage = "Gemini Vision servisi yapılandırılmamış."
                };
            }

            try
            {
                var apiUrl = $"{_settings.Endpoint.TrimEnd('/')}/{_settings.VisionModel}:generateContent?key={_settings.ApiKey}";
                var requestBody = BuildVisionRequest(imageBytes, contentType);
                var jsonContent = JsonSerializer.Serialize(requestBody);

                _logger.LogInformation("Calling Gemini Vision API: {Url}", apiUrl.Replace(_settings.ApiKey, "***"));

                // Retry with exponential backoff
                int attempt = 0;
                int delayMs = 1000;

                while (attempt < MaxRetries)
                {
                    attempt++;

                    try
                    {
                        var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                        var response = await _httpClient.PostAsync(apiUrl, httpContent);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return ParseVisionResponse(responseBody);
                        }

                        var statusCode = (int)response.StatusCode;

                        if ((statusCode == 429 || statusCode == 503) && attempt < MaxRetries)
                        {
                            _logger.LogWarning("Gemini Vision API {StatusCode}, retrying in {Delay}ms", statusCode, delayMs);
                            await Task.Delay(delayMs);
                            delayMs *= 2;
                            continue;
                        }

                        _logger.LogError("Gemini Vision API error: {StatusCode} - Response: {Body}", statusCode, responseBody);
                        return new VisionResult
                        {
                            IsSuccess = false,
                            ErrorMessage = GetErrorMessage(statusCode) + $" (Detay: {TruncateResponse(responseBody)})"
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
                _logger.LogError(ex, "Error in GeminiVisionService.AnalyzeAsync");
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi sırasında hata oluştu."
                };
            }
        }

        private static object BuildVisionRequest(byte[] imageBytes, string contentType)
        {
            var base64Image = Convert.ToBase64String(imageBytes);

            var prompt = "Bu görselde insan var mı? Varsa vücut tipini (Zayıf/Şişman/Kaslı/Normal) sınıflandır. SADECE şu JSON formatında yanıt ver: {\"isHuman\": true/false, \"bodyCategory\": \"...\", \"description\": \"1-2 cümle\"}";

            return new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new object[]
                        {
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = contentType,
                                    data = base64Image
                                }
                            },
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    maxOutputTokens = 200
                }
            };
        }

        private VisionResult ParseVisionResponse(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var content) &&
                        content.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var textContent = new StringBuilder();
                        foreach (var part in parts.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var text))
                            {
                                textContent.Append(text.GetString());
                            }
                        }

                        var jsonText = ExtractJson(textContent.ToString());
                        return ParseJsonResult(jsonText);
                    }
                }

                _logger.LogWarning("Could not parse Gemini Vision response");
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi yanıtı beklenmeyen formatta."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing Gemini Vision response");
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
                _logger.LogWarning(ex, "Could not parse vision JSON result");
                return new VisionResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Fotoğraf analizi sonucu okunamadı."
                };
            }
        }

        private static string GetErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Geçersiz fotoğraf formatı.",
                401 or 403 => "Gemini API anahtarı geçersiz.",
                404 => "Vision modeli bulunamadı.",
                429 => "Çok fazla istek. Lütfen bekleyin.",
                _ => $"Fotoğraf analizi hatası (HTTP {statusCode})."
            };
        }

        private static string TruncateResponse(string response)
        {
            if (string.IsNullOrEmpty(response)) return "";
            return response.Length > 200 ? response[..200] + "..." : response;
        }
    }
}
