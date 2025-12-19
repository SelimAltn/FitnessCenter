using System.Text.Json.Serialization;

namespace FitnessCenter.Web.Models.ViewModels
{
    /// <summary>
    /// AI öneri sonucu için ViewModel
    /// Özet, BMI, vücut kategorisi, antrenman planı, beslenme önerileri, uyarılar
    /// </summary>
    public class AiResultVm
    {
        // ===== Özet Bilgileri =====

        /// <summary>
        /// Kısa özet (BMI + kategori + hedefe göre 1-2 cümle)
        /// </summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        /// <summary>
        /// Vücut kategorisi (Data: Zayıf/Normal/Kilolu/Obez, Photo: Zayıf/Kilolu/Kaslı)
        /// </summary>
        [JsonPropertyName("bodyCategory")]
        public string? BodyCategory { get; set; }

        /// <summary>
        /// BMI değeri (sadece Data modunda hesaplanır)
        /// </summary>
        [JsonPropertyName("bmi")]
        public decimal? BMI { get; set; }

        // ===== Plan ve Öneriler =====

        /// <summary>
        /// Haftalık antrenman planı (gün gün)
        /// </summary>
        [JsonPropertyName("workoutPlan")]
        public List<string> WorkoutPlan { get; set; } = new();

        /// <summary>
        /// Beslenme önerileri
        /// </summary>
        [JsonPropertyName("nutritionTips")]
        public List<string> NutritionTips { get; set; } = new();

        /// <summary>
        /// Dikkat edilmesi gerekenler / uyarılar
        /// </summary>
        [JsonPropertyName("notes")]
        public List<string> Notes { get; set; } = new();

        // ===== Fotoğraf Analizi (Photo Modu) =====

        /// <summary>
        /// Fotoğrafta insan var mı?
        /// </summary>
        [JsonPropertyName("isHuman")]
        public bool IsHuman { get; set; } = true;

        /// <summary>
        /// Fotoğrafta ne görüldüğünün açıklaması (insan değilse)
        /// </summary>
        [JsonPropertyName("photoDescription")]
        public string? PhotoDescription { get; set; }

        // ===== Görsel Üretimi =====

        /// <summary>
        /// "Nasıl görünürüm?" görseli URL'si
        /// </summary>
        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Görsel üretim durumu: "pending", "ready", "unavailable"
        /// </summary>
        [JsonPropertyName("imageStatus")]
        public string ImageStatus { get; set; } = "unavailable";

        // ===== Durum Bilgileri =====

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
        /// Sonucun üretildiği tarih
        /// </summary>
        [JsonPropertyName("generatedAt")]
        public DateTime GeneratedAt { get; set; }

        /// <summary>
        /// Cache'den mi geldi?
        /// </summary>
        [JsonPropertyName("isCached")]
        public bool IsCached { get; set; }

        /// <summary>
        /// Ham metin çıktı (JSON parse edilemezse)
        /// </summary>
        [JsonPropertyName("rawText")]
        public string? RawText { get; set; }
    }
}
