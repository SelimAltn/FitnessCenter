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
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Gemini API call failed, returning fallback");
                        result = GenerateFallbackResponse(input, inputScenario);
                        result.ErrorMessage = "AI servisine ulaşılamadı, otomatik öneri sunuldu.";
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
                var result = JsonSerializer.Deserialize<AiResultVm>(cachedLog.ResponseJson);
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
            // Gemini API URL oluştur
            var apiUrl = $"{_settings.Endpoint}/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            // Request body oluştur
            var requestBody = BuildGeminiRequest(input, photoBytes, mimeType, inputScenario);

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Gemini API with scenario: {Scenario}", inputScenario);

            var response = await _httpClient.PostAsync(apiUrl, httpContent);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseGeminiResponse(responseJson, input, inputScenario);
        }

        private object BuildGeminiRequest(AiRecommendVm input, byte[]? photoBytes, string? mimeType, string inputScenario)
        {
            var parts = new List<object>();

            // System prompt + user prompt
            var systemPrompt = GetSystemPrompt();
            var userPrompt = BuildPrompt(input, inputScenario);
            
            parts.Add(new { text = systemPrompt + "\n\n" + userPrompt });

            // Eğer fotoğraf varsa ekle
            if (photoBytes != null && photoBytes.Length > 0 && !string.IsNullOrEmpty(mimeType))
            {
                var base64Image = Convert.ToBase64String(photoBytes);
                parts.Add(new
                {
                    inline_data = new
                    {
                        mime_type = mimeType,
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
                    maxOutputTokens = 1500,
                    responseMimeType = "application/json"
                }
            };
        }

        private static string GetSystemPrompt()
        {
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

                // Gemini response yapısı: candidates[0].content.parts[0].text
                string? content = null;
                
                if (root.TryGetProperty("candidates", out var candidates) && 
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                        contentObj.TryGetProperty("parts", out var partsArray) &&
                        partsArray.GetArrayLength() > 0)
                    {
                        var firstPart = partsArray[0];
                        if (firstPart.TryGetProperty("text", out var textProp))
                        {
                            content = textProp.GetString();
                        }
                    }
                }

                if (string.IsNullOrEmpty(content))
                {
                    throw new InvalidOperationException("Empty Gemini response");
                }

                // Content içindeki JSON'u parse et
                content = ExtractJsonFromContent(content);

                using var contentDoc = JsonDocument.Parse(content);
                var contentRoot = contentDoc.RootElement;

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
                    ModelUsed = _settings.Model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", responseJson);
                
                // Parse hatası olursa fallback döndür
                var fallback = GenerateFallbackResponse(input, inputScenario);
                fallback.ErrorMessage = "AI yanıtı işlenirken hata oluştu, alternatif öneri sunuldu.";
                return fallback;
            }
        }

        private static string ExtractJsonFromContent(string content)
        {
            content = content.Trim();
            
            // Markdown code block temizle
            if (content.StartsWith("```json"))
                content = content[7..];
            else if (content.StartsWith("```"))
                content = content[3..];
            
            if (content.EndsWith("```"))
                content = content[..^3];

            return content.Trim();
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
                result.Summary = "Fotoğrafınız değerlendirildi. Genel fitness düzeyinize göre öneriler sunuyoruz. " +
                    "Daha doğru sonuçlar için boy, kilo ve yaş bilgilerinizi de girebilirsiniz.";
                
                result.Warnings = new List<string>
                {
                    "Bu öneriler fotoğraf analizi yapılamadığı için genel niteliktedir",
                    "Kesin sonuçlar için ölçü bilgilerinizi de girmenizi öneririz",
                    "Yeni bir egzersiz programına başlamadan önce doktorunuza danışın"
                };
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
    }
}
