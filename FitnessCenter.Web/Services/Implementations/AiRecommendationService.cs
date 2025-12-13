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
    /// AI tabanlı fitness önerisi servisi implementasyonu
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
                if (input.Photo != null && input.Photo.Length > 0)
                {
                    using var ms = new MemoryStream();
                    await input.Photo.CopyToAsync(ms);
                    photoBytes = ms.ToArray();
                }

                // 2. Input hash üret
                var inputHash = GenerateInputHash(input, photoBytes);

                // 3. Cache kontrol (DB ana kaynak)
                var cachedResult = await CheckDbCacheAsync(inputHash, uyeId);
                if (cachedResult != null)
                {
                    _logger.LogInformation("Cache hit for UyeId: {UyeId}, Hash: {Hash}", uyeId, inputHash);
                    
                    // IMemoryCache'e de ekle (ikincil cache)
                    _memoryCache.Set(GetMemoryCacheKey(inputHash, uyeId), cachedResult, 
                        TimeSpan.FromHours(_settings.CacheHours));
                    
                    return cachedResult;
                }

                // 4. API yapılandırılmış mı?
                AiResultVm result;
                if (!_settings.IsConfigured)
                {
                    _logger.LogWarning("AI API key not configured, returning fallback response");
                    result = GenerateFallbackResponse(input);
                }
                else
                {
                    // 5. AI API çağrısı
                    result = await CallAiApiAsync(input);
                }

                stopwatch.Stop();

                // 6. Sonucu DB'ye kaydet
                await LogToDbAsync(input, result, uyeId, inputHash, stopwatch.ElapsedMilliseconds);

                // 7. IMemoryCache'e ekle
                _memoryCache.Set(GetMemoryCacheKey(inputHash, uyeId), result, 
                    TimeSpan.FromHours(_settings.CacheHours));

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "AI recommendation error for UyeId: {UyeId}", uyeId);

                var errorResult = new AiResultVm
                {
                    IsSuccess = false,
                    ErrorMessage = "Öneri alınırken bir hata oluştu. Lütfen tekrar deneyin.",
                    GeneratedAt = DateTime.UtcNow
                };

                // Hatayı da logla
                await LogErrorToDbAsync(input, ex.Message, uyeId, stopwatch.ElapsedMilliseconds);

                return errorResult;
            }
        }

        public string GenerateInputHash(AiRecommendVm input, byte[]? photoBytes = null)
        {
            var sb = new StringBuilder();
            sb.Append(input.Boy);
            sb.Append('|');
            sb.Append(input.Kilo);
            sb.Append('|');
            sb.Append(input.Yas);
            sb.Append('|');
            sb.Append(input.Cinsiyet ?? "");
            sb.Append('|');
            sb.Append(input.Hedef);
            sb.Append('|');
            sb.Append(input.AntrenmanGunu);
            sb.Append('|');
            sb.Append(input.Ekipman);
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

        private async Task<AiResultVm> CallAiApiAsync(AiRecommendVm input)
        {
            var prompt = BuildPrompt(input);

            var requestBody = new
            {
                model = _settings.Model,
                messages = new[]
                {
                    new { role = "system", content = GetSystemPrompt() },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 1500
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");

            var response = await _httpClient.PostAsync(_settings.Endpoint, httpContent);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            return ParseAiResponse(responseJson, input);
        }

        private static string GetSystemPrompt()
        {
            return @"Sen bir fitness ve beslenme uzmanısın. Kullanıcının fiziksel özelliklerine ve hedeflerine göre kişiselleştirilmiş antrenman ve beslenme önerisi veriyorsun.

Yanıtını SADECE aşağıdaki JSON formatında ver, başka hiçbir şey ekleme:
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
- Sağlık kısıtlarını dikkate al";
        }

        private static string BuildPrompt(AiRecommendVm input)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Kullanıcı Bilgileri:");
            sb.AppendLine($"- Boy: {input.Boy} cm");
            sb.AppendLine($"- Kilo: {input.Kilo} kg");
            sb.AppendLine($"- Yaş: {input.Yas}");
            
            if (!string.IsNullOrEmpty(input.Cinsiyet))
                sb.AppendLine($"- Cinsiyet: {input.Cinsiyet}");
            
            sb.AppendLine($"- Hedef: {input.Hedef}");
            sb.AppendLine($"- Haftalık Antrenman Günü: {input.AntrenmanGunu}");
            sb.AppendLine($"- Ekipman: {input.Ekipman}");

            if (!string.IsNullOrEmpty(input.SaglikKisiti))
                sb.AppendLine($"- Sağlık Kısıtları: {input.SaglikKisiti}");

            // BMI hesapla
            var heightM = input.Boy / 100m;
            var bmi = input.Kilo / (heightM * heightM);
            sb.AppendLine($"- BMI: {bmi:F1}");

            sb.AppendLine();
            sb.AppendLine("Bu bilgilere göre kişiselleştirilmiş antrenman planı ve beslenme önerisi ver.");

            return sb.ToString();
        }

        private AiResultVm ParseAiResponse(string responseJson, AiRecommendVm input)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // OpenAI response yapısı: choices[0].message.content
                var content = root
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrEmpty(content))
                {
                    throw new InvalidOperationException("Empty AI response");
                }

                // Content içindeki JSON'u parse et
                // Bazen markdown code block içinde gelebilir
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
                    InputSummary = BuildInputSummary(input)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI response: {Response}", responseJson);
                
                // Parse hatası olursa fallback döndür
                var fallback = GenerateFallbackResponse(input);
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

        private AiResultVm GenerateFallbackResponse(AiRecommendVm input)
        {
            var result = new AiResultVm
            {
                IsSuccess = true,
                IsCached = false,
                IsFallback = true,
                GeneratedAt = DateTime.UtcNow,
                InputSummary = BuildInputSummary(input)
            };

            // BMI hesapla
            var heightM = input.Boy / 100m;
            var bmi = input.Kilo / (heightM * heightM);

            // Özet oluştur
            var bmiCategory = bmi switch
            {
                < 18.5m => "düşük kilolu",
                < 25m => "normal kilolu",
                < 30m => "fazla kilolu",
                _ => "obez sınıfında"
            };

            result.Summary = $"BMI değeriniz {bmi:F1} olup {bmiCategory} kategorisinde yer almaktasınız. " +
                           $"{input.Hedef} hedefinize ulaşmak için haftada {input.AntrenmanGunu} gün düzenli antrenman yapmanızı öneriyoruz.";

            // Hedef bazlı antrenman planı
            result.WorkoutPlan = input.Hedef switch
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
                    "Dinlenme günlerini atlamamın, kaslar dinlenirken büyür"
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
            var ekipmanNotu = input.Ekipman switch
            {
                "Bodyweight (Alet yok)" => "Vücut ağırlığı egzersizleri: şınav, mekik, squat, plank",
                "Dumbbell (Evde ağırlık)" => "Dumbbell ile: biceps curl, shoulder press, goblet squat",
                _ => "Salon imkanlarından maksimum faydalanın"
            };
            result.WorkoutPlan.Add(ekipmanNotu);

            // Beslenme önerileri
            result.NutritionTips = input.Hedef switch
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

            // Uyarılar
            result.Warnings = new List<string>
            {
                "Bu öneriler genel niteliktedir, kişisel sağlık durumunuza göre değişebilir",
                "Yeni bir egzersiz programına başlamadan önce doktorunuza danışın"
            };

            // Sağlık kısıtı varsa ekle
            if (!string.IsNullOrEmpty(input.SaglikKisiti))
            {
                result.Warnings.Add($"Belirttiğiniz sağlık kısıtlarını ({input.SaglikKisiti}) göz önünde bulundurun");
                result.Warnings.Add("Bir fizyoterapist veya spor hekimine danışmanız önerilir");
            }

            // Yaşa göre uyarı
            if (input.Yas > 50)
            {
                result.Warnings.Add("50 yaş üstü için düşük etkili egzersizler tercih edilebilir");
            }
            else if (input.Yas < 18)
            {
                result.Warnings.Add("18 yaş altı için ağır ağırlık antrenmanları önerilmez");
            }

            return result;
        }

        private static string BuildInputSummary(AiRecommendVm input)
        {
            return $"{input.Boy}cm, {input.Kilo}kg, {input.Yas} yaş | Hedef: {input.Hedef} | " +
                   $"Antrenman: Haftada {input.AntrenmanGunu} gün | Ekipman: {input.Ekipman}";
        }

        private async Task LogToDbAsync(AiRecommendVm input, AiResultVm result, int uyeId, 
            string inputHash, long durationMs)
        {
            var log = new AiLog
            {
                UyeId = uyeId,
                SoruMetni = BuildInputSummary(input),
                CevapMetni = result.Summary,
                OlusturulmaZamani = DateTime.UtcNow,
                InputHash = inputHash,
                IsCached = false,
                ResponseJson = JsonSerializer.Serialize(result),
                ModelName = result.IsFallback ? "fallback" : _settings.Model,
                DurationMs = (int)durationMs,
                IsSuccess = result.IsSuccess
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
                SoruMetni = BuildInputSummary(input),
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
