using System.Text.Json.Serialization;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri sonucu için ViewModel
    /// JSON serialization için camelCase property isimleri kullanılır
    /// </summary>
    public class AiResultVm
    {
        /// <summary>
        /// Kısa özet (2-3 cümle)
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Antrenman planı maddeleri
        /// </summary>
        [JsonPropertyName("workoutPlan")]
        public List<string> WorkoutPlan { get; set; } = new();

        /// <summary>
        /// Beslenme önerileri
        /// </summary>
        [JsonPropertyName("nutritionTips")]
        public List<string> NutritionTips { get; set; } = new();

        /// <summary>
        /// Uyarılar ve dikkat edilmesi gerekenler
        /// </summary>
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Sonuç cache'den mi geldi?
        /// </summary>
        [JsonPropertyName("isCached")]
        public bool IsCached { get; set; }

        /// <summary>
        /// Sonucun üretildiği tarih
        /// </summary>
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Kullanıcının girdiği ölçüler (özet)
        /// </summary>
        [JsonPropertyName("inputSummary")]
        public string InputSummary { get; set; } = string.Empty;

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [JsonPropertyName("isSuccess")]
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Hata mesajı (başarısız ise)
        /// </summary>
        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Fallback yanıt mı? (API key yoksa veya hata durumunda)
        /// </summary>
        [JsonPropertyName("isFallback")]
        public bool IsFallback { get; set; }

        /// <summary>
        /// Öneri türü: "PhotoBased", "MeasurementBased", "Combined"
        /// </summary>
        [JsonPropertyName("recommendationType")]
        public string? RecommendationType { get; set; }

        /// <summary>
        /// Kullanılan AI model adı (örn: gemini-2.0-flash, fallback)
        /// </summary>
        [JsonPropertyName("modelUsed")]
        public string? ModelUsed { get; set; }

        // ===== Alakasız Fotoğraf Senaryosu İçin Yeni Alanlar =====
        
        /// <summary>
        /// Fotoğraf fitness/vücut analizi için uygun mu?
        /// PhotoOnly senaryosunda AI tarafından belirlenir.
        /// true = uygun, false = uygun değil (öneri üretilmez)
        /// </summary>
        [JsonPropertyName("isImageRelevant")]
        public bool IsImageRelevant { get; set; } = true;

        /// <summary>
        /// AI'ın fotoğraf hakkındaki açıklaması
        /// Örn: "Bu fotoğrafta bir manzara görülüyor"
        /// </summary>
        [JsonPropertyName("imageDescription")]
        public string? ImageDescription { get; set; }

        /// <summary>
        /// Fotoğrafın neden uygun/uygun olmadığının açıklaması
        /// Örn: "Vücut görünmüyor, fitness analizi yapılamaz"
        /// </summary>
        [JsonPropertyName("imageAnalysisReason")]
        public string? ImageAnalysisReason { get; set; }
    }
}
