using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// DeepSeek AI servisi - OpenAI uyumlu API
    /// Data modu: BMI hesaplama + plan
    /// Photo modu: İnsan tespiti + 3 sınıf + plan
    /// </summary>
    public class DeepSeekService : IDeepSeekService
    {
        private readonly HttpClient _httpClient;
        private readonly AiSettings _settings;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DeepSeekService> _logger;

        private const int MaxRetries = 3;
        private const int CacheHours = 24;

        public DeepSeekService(
            HttpClient httpClient,
            IOptions<AiSettings> settings,
            IMemoryCache cache,
            ILogger<DeepSeekService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _cache = cache;
            _logger = logger;

            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public bool IsConfigured => _settings.IsConfigured;

        public async Task<AiResultVm> GetRecommendationAsync(AiRecommendVm input)
        {
            if (!IsConfigured)
            {
                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "AI servisi yapılandırılmamış. Lütfen yöneticiye başvurun.",
                    GeneratedAt = DateTime.UtcNow
                };
            }

            // Cache kontrolü
            var cacheKey = GenerateCacheKey(input);
            if (_cache.TryGetValue(cacheKey, out AiResultVm? cachedResult) && cachedResult != null)
            {
                _logger.LogInformation("Cache hit for key: {Key}", cacheKey[..20] + "...");
                cachedResult.IsCached = true;
                return cachedResult;
            }

            try
            {
                AiResultVm result;

                if (input.IsPhotoMode)
                {
                    // Fotoğraf modu - şu an desteklenmiyor (DeepSeek Vision API gerekli)
                    result = new AiResultVm
                    {
                        IsSuccess = false,
                        IsHuman = false,
                        ErrorMessage = "Fotoğraf analizi şu an desteklenmiyor. Lütfen ölçü bilgilerinizi girin.",
                        GeneratedAt = DateTime.UtcNow
                    };
                }
                else
                {
                    // Data modu - BMI hesapla ve plan üret
                    result = await GetDataModeRecommendationAsync(input);
                }

                // Cache'e kaydet
                if (result.IsSuccess)
                {
                    _cache.Set(cacheKey, result, TimeSpan.FromHours(CacheHours));
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetRecommendationAsync");
                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "Beklenmeyen bir hata oluştu. Lütfen tekrar deneyin.",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        /// <summary>
        /// Photo modu: VisionResult'a göre plan üret
        /// </summary>
        public async Task<AiResultVm> GetPhotoModeRecommendationAsync(VisionResult visionResult, AiRecommendVm preferences)
        {
            if (!IsConfigured)
            {
                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "AI servisi yapılandırılmamış.",
                    GeneratedAt = DateTime.UtcNow
                };
            }

            if (!visionResult.IsHuman)
            {
                return new AiResultVm
                {
                    IsSuccess = false,
                    IsHuman = false,
                    PhotoDescription = visionResult.Description,
                    ErrorMessage = "Lütfen bir insan fotoğrafı yükleyin.",
                    GeneratedAt = DateTime.UtcNow
                };
            }

            try
            {
                var result = await CallDeepSeekForPhotoModeAsync(visionResult, preferences);
                result.IsHuman = true;
                result.BodyCategory = visionResult.BodyCategory;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetPhotoModeRecommendationAsync");
                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "Plan üretilirken hata oluştu.",
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        private async Task<AiResultVm> CallDeepSeekForPhotoModeAsync(VisionResult visionResult, AiRecommendVm preferences)
        {
            var apiUrl = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";
            var requestBody = BuildPhotoModeRequest(visionResult, preferences);
            var jsonContent = JsonSerializer.Serialize(requestBody);

            _logger.LogInformation("Calling DeepSeek API for Photo mode. Category: {Category}", visionResult.BodyCategory);

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
                        return ParseResponse(responseBody, null, visionResult.BodyCategory);
                    }

                    var statusCode = (int)response.StatusCode;

                    if ((statusCode == 429 || statusCode == 503) && attempt < MaxRetries)
                    {
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }

                    return new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = GetUserFriendlyErrorMessage(statusCode, responseBody),
                        GeneratedAt = DateTime.UtcNow
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
                    return new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = "İstek zaman aşımına uğradı.",
                        GeneratedAt = DateTime.UtcNow
                    };
                }
            }

            return new AiResultVm
            {
                IsSuccess = false,
                ErrorMessage = "Maksimum deneme sayısına ulaşıldı.",
                GeneratedAt = DateTime.UtcNow
            };
        }

        private object BuildPhotoModeRequest(VisionResult visionResult, AiRecommendVm preferences)
        {
            var userInfo = new StringBuilder();
            userInfo.AppendLine($"Fotoğraf analizi: {visionResult.Description}");
            userInfo.AppendLine($"Vücut kategorisi: {visionResult.BodyCategory}");

            if (!string.IsNullOrEmpty(preferences.Hedef))
                userInfo.AppendLine($"Hedef: {preferences.Hedef}");

            if (!string.IsNullOrEmpty(preferences.Ekipman))
                userInfo.AppendLine($"Ekipman: {preferences.Ekipman}");

            if (preferences.AntrenmanGunu.HasValue)
                userInfo.AppendLine($"Haftalık antrenman günü: {preferences.AntrenmanGunu}");

            var systemPrompt = @"Sen bir fitness uzmanısın. Fotoğraf analizi sonucuna göre kişiselleştirilmiş öneri ver.

SADECE aşağıdaki JSON formatında Türkçe yanıt ver:
{
  ""summary"": ""Vücut kategorisine ve hedefe göre 2-3 cümlelik özet"",
  ""workoutPlan"": [""Pazartesi: ..., Salı: Dinlenme, ...""],
  ""nutritionTips"": [""öneri1"", ""öneri2"", ""öneri3""],
  ""notes"": [""uyarı1"", ""uyarı2""]
}";

            return new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userInfo.ToString() }
                },
                stream = false,
                temperature = 0.7,
                max_tokens = 1024
            };
        }

        private async Task<AiResultVm> GetDataModeRecommendationAsync(AiRecommendVm input)
        {
            // BMI hesapla
            decimal? bmi = null;
            string bodyCategory = "Belirsiz";

            if (input.Boy.HasValue && input.Kilo.HasValue)
            {
                var heightM = input.Boy.Value / 100m;
                bmi = Math.Round(input.Kilo.Value / (heightM * heightM), 1);
                bodyCategory = GetBMICategory(bmi.Value);
            }

            // API çağrısı
            var result = await CallDeepSeekApiAsync(input, bmi, bodyCategory);

            // BMI bilgilerini ekle
            result.BMI = bmi;
            result.BodyCategory = bodyCategory;

            return result;
        }

        private static string GetBMICategory(decimal bmi)
        {
            return bmi switch
            {
                < 18.5m => "Zayıf",
                < 25m => "Normal",
                < 30m => "Kilolu",
                _ => "Obez"
            };
        }

        private async Task<AiResultVm> CallDeepSeekApiAsync(AiRecommendVm input, decimal? bmi, string bodyCategory)
        {
            var apiUrl = $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions";
            var requestBody = BuildDataModeRequest(input, bmi, bodyCategory);
            var jsonContent = JsonSerializer.Serialize(requestBody);

            _logger.LogInformation("Calling DeepSeek API for Data mode. BMI: {BMI}, Category: {Category}",
                bmi, bodyCategory);

            // Exponential backoff retry
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
                        return ParseResponse(responseBody, bmi, bodyCategory);
                    }

                    var statusCode = (int)response.StatusCode;

                    // 429 veya 503 ise retry
                    if ((statusCode == 429 || statusCode == 503) && attempt < MaxRetries)
                    {
                        _logger.LogWarning("DeepSeek API {StatusCode}, retrying in {Delay}ms (attempt {Attempt}/{Max})",
                            statusCode, delayMs, attempt, MaxRetries);
                        await Task.Delay(delayMs);
                        delayMs *= 2; // Exponential backoff
                        continue;
                    }

                    // Diğer hatalar
                    var errorMessage = GetUserFriendlyErrorMessage(statusCode, responseBody);
                    _logger.LogError("DeepSeek API error: {StatusCode} - {Body}", statusCode, responseBody);

                    return new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMessage,
                        BMI = bmi,
                        BodyCategory = bodyCategory,
                        GeneratedAt = DateTime.UtcNow
                    };
                }
                catch (TaskCanceledException)
                {
                    if (attempt < MaxRetries)
                    {
                        _logger.LogWarning("DeepSeek API timeout, retrying (attempt {Attempt}/{Max})", attempt, MaxRetries);
                        await Task.Delay(delayMs);
                        delayMs *= 2;
                        continue;
                    }

                    return new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.",
                        BMI = bmi,
                        BodyCategory = bodyCategory,
                        GeneratedAt = DateTime.UtcNow
                    };
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Network error calling DeepSeek API");
                    return new AiResultVm
                    {
                        IsSuccess = false,
                        ErrorMessage = "Bağlantı hatası. Lütfen internet bağlantınızı kontrol edin.",
                        BMI = bmi,
                        BodyCategory = bodyCategory,
                        GeneratedAt = DateTime.UtcNow
                    };
                }
            }

            return new AiResultVm
            {
                IsSuccess = false,
                ErrorMessage = "Maksimum deneme sayısına ulaşıldı. Lütfen daha sonra tekrar deneyin.",
                BMI = bmi,
                BodyCategory = bodyCategory,
                GeneratedAt = DateTime.UtcNow
            };
        }

        private object BuildDataModeRequest(AiRecommendVm input, decimal? bmi, string bodyCategory)
        {
            var userInfo = new StringBuilder();
            userInfo.AppendLine($"Boy: {input.Boy}cm, Kilo: {input.Kilo}kg");

            if (bmi.HasValue)
                userInfo.AppendLine($"BMI: {bmi:F1} ({bodyCategory})");

            if (input.Yas.HasValue)
                userInfo.AppendLine($"Yaş: {input.Yas}");

            if (!string.IsNullOrEmpty(input.Cinsiyet))
                userInfo.AppendLine($"Cinsiyet: {input.Cinsiyet}");

            if (!string.IsNullOrEmpty(input.Hedef))
                userInfo.AppendLine($"Hedef: {input.Hedef}");

            if (!string.IsNullOrEmpty(input.Ekipman))
                userInfo.AppendLine($"Ekipman: {input.Ekipman}");

            if (input.AntrenmanGunu.HasValue)
                userInfo.AppendLine($"Haftalık antrenman günü: {input.AntrenmanGunu}");

            var systemPrompt = @"Sen bir fitness ve beslenme uzmanısın. Kullanıcının bilgilerine göre kişiselleştirilmiş öneri ver.

SADECE aşağıdaki JSON formatında Türkçe yanıt ver:
{
  ""summary"": ""BMI değeri ve kategorisi ile hedefe göre 2-3 cümlelik özet"",
  ""workoutPlan"": [""Pazartesi: ..., Salı: Dinlenme, ...""],
  ""nutritionTips"": [""öneri1"", ""öneri2"", ""öneri3""],
  ""notes"": [""uyarı1"", ""uyarı2""]
}";

            return new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userInfo.ToString() }
                },
                stream = false,
                temperature = 0.7,
                max_tokens = 1024
            };
        }

        private string GenerateCacheKey(AiRecommendVm input)
        {
            var sb = new StringBuilder();
            sb.Append(input.IsPhotoMode ? "photo_" : "data_");
            sb.Append($"{input.Boy}_{input.Kilo}_{input.Yas}_{input.Cinsiyet}_");
            sb.Append($"{input.Hedef}_{input.Ekipman}_{input.AntrenmanGunu}");

            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return $"ai_rec_{Convert.ToHexString(hash)[..16]}";
        }

        private static string GetUserFriendlyErrorMessage(int statusCode, string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                    {
                        return $"API Hatası: {message.GetString()}";
                    }
                }
            }
            catch { }

            return statusCode switch
            {
                400 => "Geçersiz istek. Lütfen bilgilerinizi kontrol edin.",
                401 => "API anahtarı geçersiz.",
                402 => "Yetersiz bakiye. Lütfen DeepSeek hesabınızı kontrol edin.",
                429 => "Çok fazla istek. Lütfen birkaç saniye bekleyip tekrar deneyin.",
                503 => "Servis geçici olarak kullanılamıyor.",
                _ => $"Beklenmeyen hata (HTTP {statusCode})."
            };
        }

        private AiResultVm ParseResponse(string responseJson, decimal? bmi, string bodyCategory)
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
                        return ParseJsonContent(jsonText, bmi, bodyCategory);
                    }
                }

                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "AI yanıtı beklenmeyen formatta.",
                    RawText = responseJson,
                    BMI = bmi,
                    BodyCategory = bodyCategory,
                    GeneratedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing DeepSeek response");
                return new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "AI yanıtı işlenirken hata oluştu.",
                    RawText = responseJson,
                    BMI = bmi,
                    BodyCategory = bodyCategory,
                    GeneratedAt = DateTime.UtcNow
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

        private AiResultVm ParseJsonContent(string jsonContent, decimal? bmi, string bodyCategory)
        {
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                return new AiResultVm
                {
                    IsSuccess = true,
                    GeneratedAt = DateTime.UtcNow,
                    BMI = bmi,
                    BodyCategory = bodyCategory,
                    Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                    WorkoutPlan = ParseStringArray(root, "workoutPlan"),
                    NutritionTips = ParseStringArray(root, "nutritionTips"),
                    Notes = ParseStringArray(root, "notes"),
                    ImageStatus = "unavailable" // Görsel üretim henüz aktif değil
                };
            }
            catch (JsonException)
            {
                _logger.LogWarning("Could not parse JSON, returning raw text");
                return new AiResultVm
                {
                    IsSuccess = true,
                    RawText = jsonContent,
                    BMI = bmi,
                    BodyCategory = bodyCategory,
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        private static List<string> ParseStringArray(JsonElement element, string propertyName)
        {
            var result = new List<string>();

            if (element.TryGetProperty(propertyName, out var array) &&
                array.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in array.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        result.Add(value);
                    }
                }
            }

            return result;
        }
    }
}
