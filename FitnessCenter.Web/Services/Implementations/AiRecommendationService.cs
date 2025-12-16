using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Models.ViewModels;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Gemini AI tabanlı fitness önerisi servisi implementasyonu
    /// </summary>
    public class AiRecommendationService : IAiRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;
        private readonly AiSettings _settings;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<AiRecommendationService> _logger;

        public AiRecommendationService(
            HttpClient httpClient,
            AppDbContext context,
            IOptions<AiSettings> settings,
            IMemoryCache memoryCache,
            ILogger<AiRecommendationService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _settings = settings.Value;
            _memoryCache = memoryCache;
            _logger = logger;

            // HttpClient timeout ayarla
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }

        public bool IsApiConfigured => _settings.IsConfigured;

        public async Task<AiResultVm> GetRecommendationAsync(AiRecommendVm input, int uyeId)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                // 1. Foto byte'larını al (varsa)
                byte[]? photoBytes = null;
                string? photoMimeType = null;
                if (input.Photo != null && input.Photo.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await input.Photo.CopyToAsync(ms);
                    photoBytes = ms.ToArray();
                    photoMimeType = input.Photo.ContentType;
                }

                // 2. Input senaryosunu belirle
                var inputScenario = input.GetInputScenario();

                // 3. Input hash üret
                var inputHash = GenerateInputHash(input, photoBytes);

                // 4. Cache kontrol (DB ana kaynak)
                var cachedResult = await CheckDbCacheAsync(inputHash, uyeId);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for UyeId: {UyeId}, Hash: {Hash}", uyeId, inputHash);
                    
                    // IMemoryCache'e de ekle (ikincil cache)
                    _memoryCache.Set(GetMemoryCacheKey(inputHash, uyeId), cachedResult, 
                        TimeSpan.FromHours(_settings.CacheHours));
                    
                    return cachedResult;
                }

                // 5. API yapılandırılmış mı?
                AiResultVm result;
                if (!_settings.IsConfigured)
                {
                    _logger.LogWarning("AI API key not configured, returning fallback response");
                    result = GenerateFallbackResponse(input, inputScenario);
                }
                else
                {
                    // 6. Gemini API çağrısı
                    try
                    {
                        result = await CallGeminiApiAsync(input, photoBytes, photoMimeType, inputScenario);
                    }
                    catch (GeminiApiException gex)
                    {
                        _logger.LogError(gex, "Gemini API call failed with status {StatusCode}, returning fallback", gex.StatusCode);
                        result = GenerateFallbackResponse(input, inputScenario);
                        result.ErrorMessage = gex.UserMessage; // Kullanıcı dostu mesaj
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gemini API call failed with unexpected error, returning fallback");
                        result = GenerateFallbackResponse(input, inputScenario);
                        result.ErrorMessage = $"AI servisine ulaşılamadı: {ex.Message}";
                    }
                }

                stopwatch.Stop();

                // 7. Sonucu DB'ye kaydet
                await LogToDbAsync(input, result, uyeId, inputHash, stopwatch.ElapsedMilliseconds, inputScenario);

                // 8. IMemoryCache'e ekle
                _memoryCache.Set(GetMemoryCacheKey(inputHash, uyeId), result, 
                    TimeSpan.FromHours(_settings.CacheHours));

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "AI recommendation error for UyeId: {UyeId}", uyeId);

                var errorResult = GenerateFallbackResponse(input, "Error");
                errorResult.IsSuccess = true;
                errorResult.ErrorMessage = "Öneri alınırken bir hata oluştu. Sistem otomatik öneriler sunar.";

                // Hatayı da logla
                await LogErrorToDbAsync(input, ex.Message, uyeId, stopwatch.ElapsedMilliseconds);

                return errorResult;
            }
        }

        public string GenerateInputHash(AiRecommendVm input, byte[]? photoBytes = null)
        {
            var sb = new StringBuilder();
            sb.Append(input.Boy?.ToString() ?? "null");
            sb.Append('|');
            sb.Append(input.Kilo?.ToString() ?? "null");
            sb.Append('|');
            sb.Append(input.Yas?.ToString() ?? "null");
            sb.Append('|');
            sb.Append(input.Cinsiyet ?? "");
            sb.Append('|');
            sb.Append(input.Hedef ?? "");
            sb.Append('|');
            sb.Append(input.AntrenmanGunu?.ToString() ?? "null");
            sb.Append('|');
            sb.Append(input.Ekipman ?? "");
            sb.Append('|');
            sb.Append(input.SaglikKisiti ?? "");

            // Foto varsa hash'e dahil et
            if (photoBytes != null && photoBytes.Length > 0)
            {
                sb.Append('|');
                sb.Append(Convert.ToBase64String(SHA256.HashData(photoBytes)));
            }

            var inputString = sb.ToString();
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(inputString));
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        private async Task<AiResultVm?> CheckDbCacheAsync(string inputHash, int uyeId)
        {
            // Önce IMemoryCache kontrol (hızlı)
            var memoryCacheKey = GetMemoryCacheKey(inputHash, uyeId);
            if (_memoryCache.TryGetValue(memoryCacheKey, out AiResultVm? memCached))
            {
                return memCached;
            }

            // DB'den kontrol (ana kaynak)
            var cacheExpiry = DateTime.UtcNow.AddHours(-_settings.CacheHours);
            var cachedLog = await _context.AiLoglar
                .Where(l => l.UyeId == uyeId 
                         && l.InputHash == inputHash 
                         && l.IsSuccess 
                         && l.OlusturulmaZamani > cacheExpiry)
                .OrderByDescending(l => l.OlusturulmaZamani)
                .FirstOrDefaultAsync();

            if (cachedLog == null || string.IsNullOrEmpty(cachedLog.ResponseJson))
            {
                return null;
            }

            try
            {
                // Case-insensitive deserialize: eski PascalCase + yeni camelCase kayıtlar için
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AiResultVm>(cachedLog.ResponseJson, jsonOptions);
                if (result != null)
                {
                    result.IsCached = true;
                    result.GeneratedAt = cachedLog.OlusturulmaZamani;
                }
                return result;
            }
            catch
            {
                return null;
            }
        }

        private static string GetMemoryCacheKey(string inputHash, int uyeId) 
            => $"ai_recommend_{uyeId}_{inputHash}";

        private async Task<AiResultVm> CallGeminiApiAsync(AiRecommendVm input, byte[]? photoBytes, string? mimeType, string inputScenario)
        {
            // Gemini API URL oluştur (çift slash önleme)
            var baseUrl = _settings.Endpoint.TrimEnd('/');
            var apiUrl = $"{baseUrl}/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            // ===== DIAGNOSTIC LOG 1: Request bilgileri =====
            _logger.LogWarning("GEMINI CALL -> Url={Url} | Endpoint={Endpoint} | Model={Model} | KeyLen={KeyLen} | IsConfigured={IsConfigured}",
                apiUrl.Replace(_settings.ApiKey, "***REDACTED***"),
                _settings.Endpoint,
                _settings.Model,
                _settings.ApiKey?.Length ?? 0,
                _settings.IsConfigured);

            // Request body oluştur
            var requestBody = BuildGeminiRequest(input, photoBytes, mimeType, inputScenario);

            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            // ===== DIAGNOSTIC LOG 2: Request body (kısaltılmış) =====
            var bodyPreview = jsonContent.Length > 500 ? jsonContent[..500] + "..." : jsonContent;
            _logger.LogWarning("GEMINI REQUEST BODY (preview): {Body}", bodyPreview);
            
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Gemini API with scenario: {Scenario}", inputScenario);

            var response = await _httpClient.PostAsync(apiUrl, httpContent);
            
            // ===== DIAGNOSTIC LOG 3: Response bilgileri (her zaman) =====
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GEMINI RESP -> Status={Status} Body={Body}", 
                (int)response.StatusCode, 
                responseBody.Length > 1000 ? responseBody[..1000] + "..." : responseBody);
            
            if (!response.IsSuccessStatusCode)
            {
                // Kullanıcıya anlamlı hata mesajı üret
                var errorMessage = GetUserFriendlyErrorMessage((int)response.StatusCode, responseBody);
                _logger.LogError("Gemini API error: {StatusCode} - {Error} - UserMessage: {UserMessage}", 
                    response.StatusCode, responseBody, errorMessage);
                
                throw new GeminiApiException(errorMessage, (int)response.StatusCode, responseBody);
            }

            return ParseGeminiResponse(responseBody, input, inputScenario);
        }

        /// <summary>
        /// HTTP status code'a göre kullanıcı dostu hata mesajı üretir
        /// </summary>
        private static string GetUserFriendlyErrorMessage(int statusCode, string responseBody)
        {
            return statusCode switch
            {
                400 => $"İstek formatı hatalı. Gemini API isteği reddetti. Detay: {ExtractErrorMessage(responseBody)}",
                401 => "API key geçersiz veya eksik. Lütfen Gemini API anahtarınızı kontrol edin.",
                403 => "API key yetkisi yok veya geçersiz. Gemini API erişimi reddedildi.",
                404 => $"Model bulunamadı. '{ExtractErrorMessage(responseBody)}' - Model adını kontrol edin.",
                429 => "API kullanım limiti aşıldı (Quota). Lütfen daha sonra tekrar deneyin.",
                500 => "Gemini sunucu hatası. Lütfen daha sonra tekrar deneyin.",
                503 => "Gemini servisi şu an kullanılamıyor. Lütfen daha sonra tekrar deneyin.",
                _ => $"Gemini API hatası (HTTP {statusCode}). Detay: {ExtractErrorMessage(responseBody)}"
            };
        }

        /// <summary>
        /// Gemini error response'dan mesajı çıkarır
        /// </summary>
        private static string ExtractErrorMessage(string responseBody)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("error", out var error))
                {
                    if (error.TryGetProperty("message", out var message))
                    {
                        return message.GetString() ?? "Bilinmeyen hata";
                    }
                }
            }
            catch
            {
                // JSON parse edilemezse raw body'nin bir kısmını döndür
            }
            return responseBody.Length > 200 ? responseBody[..200] + "..." : responseBody;
        }

        private object BuildGeminiRequest(AiRecommendVm input, byte[]? photoBytes, string? mimeType, string inputScenario)
        {
            var parts = new List<object>();

            // System prompt + user prompt
            var systemPrompt = GetSystemPrompt(inputScenario);
            var userPrompt = BuildPrompt(input, inputScenario);
            
            parts.Add(new { text = systemPrompt + "\n\n" + userPrompt });

            // Eğer fotoğraf varsa ekle
            if (photoBytes != null && photoBytes.Length > 0 && !string.IsNullOrEmpty(mimeType))
            {
                var base64Image = Convert.ToBase64String(photoBytes);
                parts.Add(new
                {
                    inlineData = new
                    {
                        mimeType = mimeType,
                        data = base64Image
                    }
                });
            }

            return new
            {
                contents = new[]
                {
                    new { parts }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 8192, // gemini-2.5-flash thinking model için artırıldı
                    responseMimeType = "application/json"
                }
            };
        }

        private static string GetSystemPrompt(string inputScenario)
        {
            // PhotoOnly senaryosunda ek fotoğraf analizi alanlarını ekle
            if (inputScenario == "PhotoOnly")
            {
                return @"Sen bir fitness ve beslenme uzmanısın. Kullanıcının fotoğrafına göre kişiselleştirilmiş antrenman ve beslenme önerisi veriyorsun.

ÖNCELİKLE fotoğrafı analiz et:
1. Fotoğrafta ne var? (kısa açıklama)
2. Bu fotoğraf fitness/vücut analizi için uygun mu? (insan vücudu net görünüyor mu?)

SADECE aşağıdaki JSON formatında yanıt ver, başka hiçbir şey ekleme, markdown kullanma, code block kullanma:
{
  ""isImageRelevant"": true veya false,
  ""imageDescription"": ""Fotoğrafta görülenin kısa açıklaması"",
  ""imageAnalysisReason"": ""Neden uygun veya uygun değil açıklaması"",
  ""summary"": ""2-3 cümlelik özet (sadece isImageRelevant=true ise doldur)"",
  ""workoutPlan"": [""madde1"", ""madde2"", ...] (sadece isImageRelevant=true ise doldur),
  ""nutritionTips"": [""madde1"", ""madde2"", ...] (sadece isImageRelevant=true ise doldur),
  ""warnings"": [""madde1"", ""madde2"", ...] (sadece isImageRelevant=true ise doldur)
}

Kurallar:
- Türkçe yaz
- isImageRelevant=false ise summary/workoutPlan/nutritionTips/warnings BOŞ ARRAY olarak döndür
- isImageRelevant=true ise tüm alanları doldur
- Her liste maksimum 6 madde olsun
- Kısa ve net cümleler kullan
- Fotoğrafta insan vücudu net görünmüyorsa isImageRelevant=false olmalı
- Manzara, yemek, hayvan, nesne gibi fitness ile alakasız görseller için isImageRelevant=false";
            }

            // Normal senaryo (DataOnly veya Combined)
            return @"Sen bir fitness ve beslenme uzmanısın. Kullanıcının fiziksel özelliklerine, hedeflerine ve/veya fotoğrafına göre kişiselleştirilmiş antrenman ve beslenme önerisi veriyorsun.

SADECE aşağıdaki JSON formatında yanıt ver, başka hiçbir şey ekleme, markdown kullanma, code block kullanma:
{
  ""summary"": ""2-3 cümlelik özet"",
  ""workoutPlan"": [""madde1"", ""madde2"", ...],
  ""nutritionTips"": [""madde1"", ""madde2"", ...],
  ""warnings"": [""madde1"", ""madde2"", ...]
}

Kurallar:
- Türkçe yaz
- Her liste maksimum 6 madde olsun
- Kısa ve net cümleler kullan
- Sağlık kısıtlarını dikkate al
- Fotoğraf varsa vücut tipini analiz et ama kesin boy/kilo tahmini yapma";
        }

        private static string BuildPrompt(AiRecommendVm input, string inputScenario)
        {
            var sb = new StringBuilder();

            switch (inputScenario)
            {
                case "PhotoOnly":
                    sb.AppendLine("📷 FOTOĞRAF ANALİZİ MODU");
                    sb.AppendLine("Kullanıcı sadece fotoğraf yükledi, ölçü bilgisi vermedi.");
                    sb.AppendLine("Fotoğraftan vücut tipini analiz ederek genel öneri ver.");
                    sb.AppendLine("DİKKAT: Kesin boy/kilo tahmini yapma, sadece görsel değerlendirme yap.");
                    sb.AppendLine();
                    
                    if (!string.IsNullOrEmpty(input.Hedef))
                        sb.AppendLine($"- Hedef: {input.Hedef}");
                    else
                        sb.AppendLine("- Hedef: Genel fitness");
                    
                    if (input.AntrenmanGunu.HasValue)
                        sb.AppendLine($"- Haftalık Antrenman Günü: {input.AntrenmanGunu}");
                    
                    if (!string.IsNullOrEmpty(input.Ekipman))
                        sb.AppendLine($"- Ekipman: {input.Ekipman}");
                    
                    if (!string.IsNullOrEmpty(input.Cinsiyet))
                        sb.AppendLine($"- Cinsiyet: {input.Cinsiyet}");
                    
                    if (!string.IsNullOrEmpty(input.SaglikKisiti))
                        sb.AppendLine($"- Sağlık Kısıtları: {input.SaglikKisiti}");
                    break;

                case "Combined":
                    sb.AppendLine("📷📊 KOMBİNE ANALİZ MODU");
                    sb.AppendLine("Kullanıcı hem fotoğraf hem ölçü bilgileri verdi.");
                    sb.AppendLine("Fotoğraf + ölçüler birlikte değerlendirilerek en iyi öneri verilecek.");
                    sb.AppendLine();
                    sb.AppendLine("Kullanıcı Bilgileri:");
                    sb.AppendLine($"- Boy: {input.Boy} cm");
                    sb.AppendLine($"- Kilo: {input.Kilo} kg");
                    sb.AppendLine($"- Yaş: {input.Yas}");
                    
                    if (!string.IsNullOrEmpty(input.Cinsiyet))
                        sb.AppendLine($"- Cinsiyet: {input.Cinsiyet}");
                    
                    sb.AppendLine($"- Hedef: {input.Hedef ?? "Genel fitness"}");
                    sb.AppendLine($"- Haftalık Antrenman Günü: {input.AntrenmanGunu ?? 3}");
                    sb.AppendLine($"- Ekipman: {input.Ekipman ?? "Gym"}");

                    if (!string.IsNullOrEmpty(input.SaglikKisiti))
                        sb.AppendLine($"- Sağlık Kısıtları: {input.SaglikKisiti}");

                    // BMI hesapla
                    if (input.Boy.HasValue && input.Kilo.HasValue)
                    {
                        var heightM = input.Boy.Value / 100m;
                        var bmi = input.Kilo.Value / (heightM * heightM);
                        sb.AppendLine($"- BMI: {bmi:F1}");
                    }
                    break;

                default: // DataOnly
                    sb.AppendLine("📊 ÖLÇÜ BİLGİSİ MODU");
                    sb.AppendLine("Kullanıcı Bilgileri:");
                    sb.AppendLine($"- Boy: {input.Boy} cm");
                    sb.AppendLine($"- Kilo: {input.Kilo} kg");
                    sb.AppendLine($"- Yaş: {input.Yas}");
                    
                    if (!string.IsNullOrEmpty(input.Cinsiyet))
                        sb.AppendLine($"- Cinsiyet: {input.Cinsiyet}");
                    
                    sb.AppendLine($"- Hedef: {input.Hedef ?? "Genel fitness"}");
                    sb.AppendLine($"- Haftalık Antrenman Günü: {input.AntrenmanGunu ?? 3}");
                    sb.AppendLine($"- Ekipman: {input.Ekipman ?? "Gym"}");

                    if (!string.IsNullOrEmpty(input.SaglikKisiti))
                        sb.AppendLine($"- Sağlık Kısıtları: {input.SaglikKisiti}");

                    // BMI hesapla
                    if (input.Boy.HasValue && input.Kilo.HasValue)
                    {
                        var heightM = input.Boy.Value / 100m;
                        var bmi = input.Kilo.Value / (heightM * heightM);
                        sb.AppendLine($"- BMI: {bmi:F1}");
                    }
                    break;
            }

            sb.AppendLine();
            sb.AppendLine("Bu bilgilere göre kişiselleştirilmiş antrenman planı ve beslenme önerisi ver.");

            return sb.ToString();
        }

        private AiResultVm ParseGeminiResponse(string responseJson, AiRecommendVm input, string inputScenario)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // ===== DÜZELTME: Tüm parts içindeki text'leri birleştir =====
                // Gemini bazen yanıtı birden fazla part halinde döndürebiliyor
                var contentBuilder = new StringBuilder();
                
                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    
                    // finishReason kontrolü - eğer SAFETY, MAX_TOKENS veya hata varsa logla
                    if (firstCandidate.TryGetProperty("finishReason", out var finishReason))
                    {
                        var reason = finishReason.GetString();
                        _logger.LogWarning("GEMINI finishReason: {Reason}", reason);
                        
                        if (reason == "SAFETY" || reason == "RECITATION" || reason == "OTHER")
                        {
                            _logger.LogWarning("Gemini response blocked due to: {Reason}", reason);
                            throw new InvalidOperationException($"Gemini yanıtı güvenlik nedeniyle engellendi: {reason}");
                        }
                        
                        // MAX_TOKENS - yanıt yarım kaldı, JSON eksik olacak
                        if (reason == "MAX_TOKENS")
                        {
                            _logger.LogError("Gemini response truncated due to MAX_TOKENS limit!");
                            throw new InvalidOperationException("Gemini yanıtı token limiti nedeniyle tamamlanamadı. Lütfen tekrar deneyin.");
                        }
                    }
                    
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var partsArray) &&
                        partsArray.GetArrayLength() > 0)
                    {
                        // ===== TÜM PARTS'LARI BİRLEŞTİR =====
                        foreach (var part in partsArray.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textProp))
                            {
                                var textValue = textProp.GetString();
                                if (!string.IsNullOrEmpty(textValue))
                                {
                                    contentBuilder.Append(textValue);
                                }
                            }
                        }
                        
                        _logger.LogInformation("GEMINI PARSE: Combined {PartCount} parts into {TotalLength} chars", 
                            partsArray.GetArrayLength(), contentBuilder.Length);
                    }
                }

                var content = contentBuilder.ToString();
                
                if (string.IsNullOrEmpty(content))
                {
                    _logger.LogError("GEMINI PARSE ERROR: No text content found in response. Raw: {Raw}", 
                        responseJson.Length > 500 ? responseJson[..500] : responseJson);
                    throw new InvalidOperationException("Empty Gemini response - parts içinde text bulunamadı");
                }

                // ===== DÜZELTME: JSON extraction daha sağlam =====
                _logger.LogWarning("GEMINI RAW CONTENT (before extract): {Content}", 
                    content.Length > 300 ? content[..300] + "..." : content);
                
                content = ExtractJsonFromContent(content);
                
                _logger.LogWarning("GEMINI EXTRACTED JSON: {Json}", 
                    content.Length > 300 ? content[..300] + "..." : content);

                // JSON parse denemesi
                JsonDocument contentDoc;
                try
                {
                    contentDoc = JsonDocument.Parse(content);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "GEMINI JSON PARSE FAILED. Content is not valid JSON: {Content}", content);
                    throw new InvalidOperationException($"Gemini yanıtı geçerli JSON değil: {jsonEx.Message}");
                }
                
                using (contentDoc)
                {
                    var contentRoot = contentDoc.RootElement;

                    // PhotoOnly senaryosunda ek alanları parse et
                    bool isImageRelevant = true;
                    string? imageDescription = null;
                    string? imageAnalysisReason = null;

                    if (inputScenario == "PhotoOnly")
                    {
                        // isImageRelevant alanını kontrol et
                        if (contentRoot.TryGetProperty("isImageRelevant", out var relevantProp))
                        {
                            isImageRelevant = relevantProp.GetBoolean();
                        }

                        if (contentRoot.TryGetProperty("imageDescription", out var descProp))
                        {
                            imageDescription = descProp.GetString();
                        }

                        if (contentRoot.TryGetProperty("imageAnalysisReason", out var reasonProp))
                        {
                            imageAnalysisReason = reasonProp.GetString();
                        }

                        _logger.LogInformation("PhotoOnly analysis: IsRelevant={IsRelevant}, Description={Desc}", 
                            isImageRelevant, imageDescription);

                        // Eğer fotoğraf uygun değilse, öneri üretme (boş döndür)
                        if (!isImageRelevant)
                        {
                            return new AiResultVm
                            {
                                IsSuccess = false, // Başarılı öneri sayılmaz
                                IsImageRelevant = false,
                                ImageDescription = imageDescription,
                                ImageAnalysisReason = imageAnalysisReason,
                                Summary = "", // Boş
                                WorkoutPlan = new List<string>(), // Boş
                                NutritionTips = new List<string>(), // Boş
                                Warnings = new List<string>(), // Boş
                                IsCached = false,
                                IsFallback = false,
                                GeneratedAt = DateTime.UtcNow,
                                InputSummary = BuildInputSummary(input, inputScenario),
                                RecommendationType = "Fotoğraf Uygun Değil",
                                ModelUsed = _settings.Model
                            };
                        }
                    }

                    return new AiResultVm
                    {
                        Summary = contentRoot.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                        WorkoutPlan = ParseStringArray(contentRoot, "workoutPlan"),
                        NutritionTips = ParseStringArray(contentRoot, "nutritionTips"),
                        Warnings = ParseStringArray(contentRoot, "warnings"),
                        IsSuccess = true,
                        IsCached = false,
                        IsFallback = false,
                        GeneratedAt = DateTime.UtcNow,
                        InputSummary = BuildInputSummary(input, inputScenario),
                        RecommendationType = GetRecommendationTypeLabel(inputScenario),
                        ModelUsed = _settings.Model,
                        // PhotoOnly senaryosunda ek alanlar
                        IsImageRelevant = isImageRelevant,
                        ImageDescription = imageDescription,
                        ImageAnalysisReason = imageAnalysisReason
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", responseJson);
                
                // Parse hatası olursa fallback döndür
                var fallback = GenerateFallbackResponse(input, inputScenario);
                fallback.ErrorMessage = $"AI yanıtı işlenirken hata oluştu: {ex.Message}. Alternatif öneri sunuldu.";
                return fallback;
            }
        }

        private string ExtractJsonFromContent(string content)
        {
            content = content.Trim();
            
            // Markdown code block temizle
            if (content.StartsWith("```json"))
                content = content[7..];
            else if (content.StartsWith("```"))
                content = content[3..];
            
            if (content.EndsWith("```"))
                content = content[..^3];

            content = content.Trim();
            
            // Eğer hala { ile başlamıyorsa, içinden JSON objesini bulmaya çalış
            if (!content.StartsWith("{"))
            {
                _logger.LogWarning("Content does not start with {{, trying to extract JSON object...");
                
                // İlk { karakterini bul
                var startIndex = content.IndexOf('{');
                if (startIndex >= 0)
                {
                    // Son } karakterini bul
                    var endIndex = content.LastIndexOf('}');
                    if (endIndex > startIndex)
                    {
                        var extracted = content.Substring(startIndex, endIndex - startIndex + 1);
                        _logger.LogInformation("Extracted JSON from content: StartIdx={Start}, EndIdx={End}", startIndex, endIndex);
                        return extracted;
                    }
                }
                
                _logger.LogWarning("Could not find JSON object in content, returning as-is");
            }

            return content;
        }

        private static List<string> ParseStringArray(JsonElement element, string propertyName)
        {
            var list = new List<string>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    var value = item.GetString();
                    if (!string.IsNullOrEmpty(value))
                        list.Add(value);
                }
            }
            return list;
        }

        private static string GetRecommendationTypeLabel(string inputScenario)
        {
            return inputScenario switch
            {
                "PhotoOnly" => "Fotoğraf Analizi",
                "Combined" => "Fotoğraf + Ölçüler",
                "DataOnly" => "Ölçü Bilgileri",
                _ => "Genel Öneri"
            };
        }

        private AiResultVm GenerateFallbackResponse(AiRecommendVm input, string inputScenario)
        {
            var result = new AiResultVm
            {
                IsSuccess = true,
                IsCached = false,
                IsFallback = true,
                GeneratedAt = DateTime.UtcNow,
                InputSummary = BuildInputSummary(input, inputScenario),
                RecommendationType = GetRecommendationTypeLabel(inputScenario) + " (Fallback)",
                ModelUsed = "fallback"
            };

            // Senaryoya göre özet
            if (inputScenario == "PhotoOnly")
            {
                // PhotoOnly senaryosunda fallback = fotoğraf analiz edilemedi
                // Öneri üretme, kullanıcıya uygunluk kontrolü yapılamadığını bildir
                result.IsSuccess = false;
                result.IsImageRelevant = false;
                result.ImageDescription = "Fotoğraf analiz edilemedi";
                result.ImageAnalysisReason = "AI servisi şu an kullanılamıyor. Fotoğraf analizi yapılamadı.";
                result.Summary = "";
                result.WorkoutPlan = new List<string>();
                result.NutritionTips = new List<string>();
                result.Warnings = new List<string>();
                result.RecommendationType = "Fotoğraf Analizi Başarısız";
                return result; // Erken dön, öneri üretme
            }
            else if (input.Boy.HasValue && input.Kilo.HasValue)
            {
                // BMI hesapla
                var heightM = input.Boy.Value / 100m;
                var bmi = input.Kilo.Value / (heightM * heightM);

                var bmiCategory = bmi switch
                {
                    < 18.5m => "düşük kilolu",
                    < 25m => "normal kilolu",
                    < 30m => "fazla kilolu",
                    _ => "obez sınıfında"
                };

                var hedef = input.Hedef ?? "Fit Kalma";
                var antrenmanGunu = input.AntrenmanGunu ?? 3;

                result.Summary = $"BMI değeriniz {bmi:F1} olup {bmiCategory} kategorisinde yer almaktasınız. " +
                               $"{hedef} hedefinize ulaşmak için haftada {antrenmanGunu} gün düzenli antrenman yapmanızı öneriyoruz.";
                
                result.Warnings = new List<string>
                {
                    "Bu öneriler genel niteliktedir, kişisel sağlık durumunuza göre değişebilir",
                    "Yeni bir egzersiz programına başlamadan önce doktorunuza danışın"
                };
            }
            else
            {
                result.Summary = "Genel fitness önerileri sunuyoruz. Daha kişiselleştirilmiş öneriler için " +
                    "boy, kilo ve yaş bilgilerinizi girmenizi öneririz.";
                
                result.Warnings = new List<string>
                {
                    "Bu öneriler genel niteliktedir",
                    "Yeni bir programa başlamadan önce doktorunuza danışın"
                };
            }

            // Hedef bazlı antrenman planı
            var targetHedef = input.Hedef ?? "Fit Kalma";
            result.WorkoutPlan = targetHedef switch
            {
                "Kilo Verme" => new List<string>
                {
                    "Haftada en az 150 dakika orta yoğunlukta kardiyo yapın",
                    "HIIT antrenmanları yağ yakımını hızlandırır",
                    "Güç antrenmanlarını ihmal etmeyin, kas kütlesi metabolizmayı artırır",
                    "Yürüyüş, bisiklet veya yüzme ile başlayabilirsiniz",
                    "Her antrenman öncesi 5-10 dakika ısınma yapın"
                },
                "Kas Kazanma" => new List<string>
                {
                    "Haftada 3-4 gün ağırlık antrenmanı yapın",
                    "Her kas grubunu haftada 2 kez çalıştırın",
                    "8-12 tekrar aralığında çalışın (hipertrofi)",
                    "Progresif yüklenme prensibini uygulayın",
                    "Dinlenme günlerini atlamayın, kaslar dinlenirken büyür"
                },
                _ => new List<string>
                {
                    "Kardiyo ve güç antrenmanlarını dengeli kombine edin",
                    "Haftada 3-4 gün düzenli egzersiz yapın",
                    "Esneklik çalışmalarını ihmal etmeyin",
                    "Aktif yaşam tarzını benimseyin",
                    "Spor aktiviteleri ile egzersizi eğlenceli hale getirin"
                }
            };

            // Ekipmana göre notlar ekle
            var ekipman = input.Ekipman ?? "Gym (Salon erişimi)";
            var ekipmanNotu = ekipman switch
            {
                "Bodyweight (Alet yok)" => "Vücut ağırlığı egzersizleri: şınav, mekik, squat, plank",
                "Dumbbell (Evde ağırlık)" => "Dumbbell ile: biceps curl, shoulder press, goblet squat",
                _ => "Salon imkanlarından maksimum faydalanın"
            };
            result.WorkoutPlan.Add(ekipmanNotu);

            // Beslenme önerileri
            result.NutritionTips = targetHedef switch
            {
                "Kilo Verme" => new List<string>
                {
                    "Günlük kalori açığı oluşturun (300-500 kcal)",
                    "Protein alımını artırın (kg başına 1.2-1.5g)",
                    "İşlenmiş gıdalardan kaçının",
                    "Bol su için (günde en az 2-3 litre)",
                    "Öğün atlamayın, porsiyon kontrolüne dikkat edin",
                    "Şekerli içecekleri kesin"
                },
                "Kas Kazanma" => new List<string>
                {
                    "Günlük kalori fazlası oluşturun (300-500 kcal)",
                    "Protein alımını artırın (kg başına 1.6-2.2g)",
                    "Kompleks karbonhidratları tercih edin",
                    "Antrenman sonrası protein alımına dikkat edin",
                    "Sağlıklı yağları ihmal etmeyin",
                    "Yeterli uyku alın (7-9 saat)"
                },
                _ => new List<string>
                {
                    "Dengeli ve çeşitli beslenin",
                    "Protein, karbonhidrat ve yağ dengesine dikkat edin",
                    "İşlenmiş gıdalardan kaçının",
                    "Bol sebze ve meyve tüketin",
                    "Günde en az 2 litre su için"
                }
            };

            // Sağlık kısıtı varsa ekle
            if (!string.IsNullOrEmpty(input.SaglikKisiti))
            {
                result.Warnings.Add($"Belirttiğiniz sağlık kısıtlarını ({input.SaglikKisiti}) göz önünde bulundurun");
                result.Warnings.Add("Bir fizyoterapist veya spor hekimine danışmanız önerilir");
            }

            // Yaşa göre uyarı
            if (input.Yas.HasValue)
            {
                if (input.Yas > 50)
                {
                    result.Warnings.Add("50 yaş üstü için düşük etkili egzersizler tercih edilebilir");
                }
                else if (input.Yas < 18)
                {
                    result.Warnings.Add("18 yaş altı için ağır ağırlık antrenmanları önerilmez");
                }
            }

            return result;
        }

        private static string BuildInputSummary(AiRecommendVm input, string inputScenario)
        {
            var sb = new StringBuilder();

            if (inputScenario == "PhotoOnly")
            {
                sb.Append("📷 Fotoğraf ile analiz");
            }
            else if (input.Boy.HasValue && input.Kilo.HasValue && input.Yas.HasValue)
            {
                sb.Append($"{input.Boy}cm, {input.Kilo}kg, {input.Yas} yaş");
            }

            if (!string.IsNullOrEmpty(input.Hedef))
            {
                sb.Append($" | Hedef: {input.Hedef}");
            }

            if (input.AntrenmanGunu.HasValue)
            {
                sb.Append($" | Haftada {input.AntrenmanGunu} gün");
            }

            if (!string.IsNullOrEmpty(input.Ekipman))
            {
                sb.Append($" | {input.Ekipman}");
            }

            if (inputScenario == "Combined")
            {
                sb.Append(" | 📷+📊");
            }

            return sb.ToString();
        }

        private async Task LogToDbAsync(AiRecommendVm input, AiResultVm result, int uyeId, 
            string inputHash, long durationMs, string inputScenario)
        {
            var log = new AiLog
            {
                UyeId = uyeId,
                SoruMetni = BuildInputSummary(input, inputScenario),
                CevapMetni = result.Summary,
                OlusturulmaZamani = DateTime.UtcNow,
                InputHash = inputHash,
                IsCached = false,
                ResponseJson = JsonSerializer.Serialize(result),
                ModelName = result.IsFallback ? "fallback" : _settings.Model,
                DurationMs = (int)durationMs,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage
            };

            _context.AiLoglar.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task LogErrorToDbAsync(AiRecommendVm input, string errorMessage, 
            int uyeId, long durationMs)
        {
            var log = new AiLog
            {
                UyeId = uyeId,
                SoruMetni = BuildInputSummary(input, input.GetInputScenario()),
                CevapMetni = "Hata oluştu",
                OlusturulmaZamani = DateTime.UtcNow,
                InputHash = null,
                IsCached = false,
                ErrorMessage = errorMessage.Length > 1000 ? errorMessage[..1000] : errorMessage,
                ModelName = _settings.Model,
                DurationMs = (int)durationMs,
                IsSuccess = false
            };

            _context.AiLoglar.Add(log);
            await _context.SaveChangesAsync();
        }

        // ===== AJAX Polling İçin Yeni Metodlar =====

        /// <summary>
        /// AI öneri işlemini arka planda başlatır.
        /// Hemen döner, işlem arka planda devam eder.
        /// </summary>
        public async Task StartRecommendationAsync(AiRecommendVm input, int uyeId, string requestId)
        {
            _logger.LogInformation("Starting background recommendation for RequestId: {RequestId}", requestId);

            // 1. Foto byte'larını al (varsa)
            byte[]? photoBytes = null;
            string? photoMimeType = null;
            if (input.Photo != null && input.Photo.Length > 0)
            {
                using var ms = new MemoryStream();
                await input.Photo.CopyToAsync(ms);
                photoBytes = ms.ToArray();
                photoMimeType = input.Photo.ContentType;
            }

            // 2. Input senaryosunu belirle
            var inputScenario = input.GetInputScenario();

            // 3. Input hash üret
            var inputHash = GenerateInputHash(input, photoBytes);

            // 4. DB'ye "Processing" status ile kayıt aç
            var log = new AiLog
            {
                UyeId = uyeId,
                SoruMetni = BuildInputSummary(input, inputScenario),
                CevapMetni = "İşleniyor...",
                OlusturulmaZamani = DateTime.UtcNow,
                InputHash = inputHash,
                IsCached = false,
                ModelName = _settings.Model,
                IsSuccess = false,
                RequestId = requestId,
                Status = "Processing"
            };
            _context.AiLoglar.Add(log);
            await _context.SaveChangesAsync();

            // 5. Input verilerini memory cache'e kaydet (background erişimi için)
            var cacheKey = $"ai_pending_{requestId}";
            var pendingData = new PendingRecommendationData
            {
                Input = input,
                UyeId = uyeId,
                PhotoBytes = photoBytes,
                PhotoMimeType = photoMimeType,
                InputScenario = inputScenario,
                InputHash = inputHash,
                LogId = log.Id
            };
            _memoryCache.Set(cacheKey, pendingData, TimeSpan.FromMinutes(10));

            _logger.LogInformation("Background recommendation queued for RequestId: {RequestId}, LogId: {LogId}", 
                requestId, log.Id);
        }

        /// <summary>
        /// Belirtilen requestId için işlem durumunu sorgular ve (gerekirse) işlemi gerçekleştirir.
        /// </summary>
        public async Task<(string Status, AiResultVm? Result, string? ErrorMessage)> GetRecommendationStatusAsync(string requestId)
        {
            // 1. DB'den mevcut kayıt durumunu kontrol et
            var existingLog = await _context.AiLoglar
                .Where(l => l.RequestId == requestId)
                .OrderByDescending(l => l.OlusturulmaZamani)
                .FirstOrDefaultAsync();

            if (existingLog == null)
            {
                return ("NotFound", null, "İstek bulunamadı.");
            }

            // 2. Eğer zaten tamamlandıysa, sonucu döndür
            if (existingLog.Status == "Completed" && !string.IsNullOrEmpty(existingLog.ResponseJson))
            {
                try
                {
                    // Case-insensitive deserialize: eski PascalCase + yeni camelCase kayıtlar için
                    var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var cachedResult = JsonSerializer.Deserialize<AiResultVm>(existingLog.ResponseJson, jsonOptions);
                    return ("Completed", cachedResult, null);
                }
                catch
                {
                    return ("Error", null, "Sonuç işlenirken hata oluştu.");
                }
            }

            // 3. Eğer hata durumundaysa
            if (existingLog.Status == "Error")
            {
                return ("Error", null, existingLog.ErrorMessage ?? "Bilinmeyen hata oluştu.");
            }

            // 4. Eğer hala işleniyorsa, gerçek işlemi yap
            if (existingLog.Status == "Processing")
            {
                // Memory cache'den pending data'yı al
                var cacheKey = $"ai_pending_{requestId}";
                if (!_memoryCache.TryGetValue(cacheKey, out PendingRecommendationData? pendingData) || pendingData == null)
                {
                    // Cache expired veya bulunamadı - hata olarak işaretle
                    existingLog.Status = "Error";
                    existingLog.ErrorMessage = "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.";
                    await _context.SaveChangesAsync();
                    return ("Error", null, existingLog.ErrorMessage);
                }

                var stopwatch = Stopwatch.StartNew();

                try
                {
                    // 5. Cache kontrolü (aynı input hash varsa hızlı dön)
                    var cachedResult = await CheckDbCacheAsync(pendingData.InputHash, pendingData.UyeId);
                    if (cachedResult != null)
                    {
                        _logger.LogInformation("Cache hit during polling for RequestId: {RequestId}", requestId);
                        
                        existingLog.Status = "Completed";
                        existingLog.CevapMetni = cachedResult.Summary;
                        existingLog.ResponseJson = JsonSerializer.Serialize(cachedResult);
                        existingLog.IsSuccess = true;
                        existingLog.IsCached = true;
                        existingLog.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                        await _context.SaveChangesAsync();

                        // Cache'den temizle
                        _memoryCache.Remove(cacheKey);

                        return ("Completed", cachedResult, null);
                    }

                    // 6. API yapılandırılmış mı?
                    AiResultVm result;
                    if (!_settings.IsConfigured)
                    {
                        _logger.LogWarning("AI API key not configured during polling, returning fallback");
                        result = GenerateFallbackResponse(pendingData.Input, pendingData.InputScenario);
                    }
                    else
                    {
                        // 7. Gemini API çağrısı
                        try
                        {
                            result = await CallGeminiApiAsync(
                                pendingData.Input, 
                                pendingData.PhotoBytes, 
                                pendingData.PhotoMimeType, 
                                pendingData.InputScenario);
                        }
                        catch (GeminiApiException gex)
                        {
                            _logger.LogError(gex, "Gemini API call failed during polling with status {StatusCode}", gex.StatusCode);
                            result = GenerateFallbackResponse(pendingData.Input, pendingData.InputScenario);
                            result.ErrorMessage = gex.UserMessage;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Gemini API call failed during polling with unexpected error");
                            result = GenerateFallbackResponse(pendingData.Input, pendingData.InputScenario);
                            result.ErrorMessage = $"AI servisine ulaşılamadı: {ex.Message}";
                        }
                    }

                    stopwatch.Stop();

                    // 8. DB'yi güncelle
                    existingLog.Status = "Completed";
                    existingLog.CevapMetni = result.Summary;
                    existingLog.ResponseJson = JsonSerializer.Serialize(result);
                    existingLog.IsSuccess = result.IsSuccess;
                    existingLog.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                    existingLog.ErrorMessage = result.ErrorMessage;
                    await _context.SaveChangesAsync();

                    // 9. Memory cache'e ekle (ikincil cache)
                    _memoryCache.Set(GetMemoryCacheKey(pendingData.InputHash, pendingData.UyeId), result, 
                        TimeSpan.FromHours(_settings.CacheHours));

                    // Pending data'yı temizle
                    _memoryCache.Remove(cacheKey);

                    return ("Completed", result, null);
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    _logger.LogError(ex, "Error during GetRecommendationStatusAsync for RequestId: {RequestId}", requestId);

                    existingLog.Status = "Error";
                    existingLog.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
                    existingLog.DurationMs = (int)stopwatch.ElapsedMilliseconds;
                    await _context.SaveChangesAsync();

                    // Cache temizle
                    _memoryCache.Remove(cacheKey);

                    return ("Error", null, $"İşlem sırasında hata oluştu: {ex.Message}");
                }
            }

            return ("Pending", null, null);
        }
    }

    /// <summary>
    /// Background processing için pending data yapısı
    /// </summary>
    public class PendingRecommendationData
    {
        public AiRecommendVm Input { get; set; } = null!;
        public int UyeId { get; set; }
        public byte[]? PhotoBytes { get; set; }
        public string? PhotoMimeType { get; set; }
        public string InputScenario { get; set; } = "";
        public string InputHash { get; set; } = "";
        public int LogId { get; set; }
    }

    /// <summary>
    /// Gemini API hatalarını temsil eden özel exception sınıfı
    /// </summary>
    public class GeminiApiException : Exception
    {
        public int StatusCode { get; }
        public string UserMessage { get; }
        public string RawResponse { get; }

        public GeminiApiException(string userMessage, int statusCode, string rawResponse)
            : base($"Gemini API error (HTTP {statusCode}): {userMessage}")
        {
            UserMessage = userMessage;
            StatusCode = statusCode;
            RawResponse = rawResponse;
        }
    }
}
