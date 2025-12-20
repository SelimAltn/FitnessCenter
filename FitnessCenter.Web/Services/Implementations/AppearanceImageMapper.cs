namespace FitnessCenter.Web.Services.Implementations
{
    /// <summary>
    /// Kural tabanlı Before/After görsel eşleştirme servisi
    /// Hiçbir AI API çağrısı yapmaz, tamamen lokal mapping
    /// </summary>
    public class AppearanceImageMapper
    {
        private const string BasePath = "/images/transformations";

        /// <summary>
        /// Vücut kategorisi, hedef ve cinsiyete göre görsel yollarını döndürür
        /// </summary>
        public TransformationImageResult GetTransformationImages(
            string? bodyCategory,
            string? goal,
            string? gender)
        {
            var genderSuffix = GetGenderSuffix(gender);
            var normalizedCategory = NormalizeCategory(bodyCategory);
            var normalizedGoal = NormalizeGoal(goal);

            // Before image - mevcut duruma göre
            var beforeImage = $"{BasePath}/before/{normalizedCategory}_{genderSuffix}.png";

            // After image - hedefe ve mevcut duruma göre
            var afterType = GetAfterType(normalizedCategory, normalizedGoal);
            var afterImage = $"{BasePath}/after/{afterType}_{genderSuffix}.png";

            return new TransformationImageResult
            {
                BeforePath = beforeImage,
                AfterPath = afterImage,
                Caption = "Bu görseller temsili olup bilgilendirme amaçlıdır. Gerçek sonuçlar kişiden kişiye değişebilir."
            };
        }

        /// <summary>
        /// Cinsiyet suffix'i belirle (varsayılan: male)
        /// </summary>
        private static string GetGenderSuffix(string? gender)
        {
            if (string.IsNullOrEmpty(gender))
                return "male";

            return gender.ToLowerInvariant() switch
            {
                "kadın" => "female",
                "female" => "female",
                _ => "male"
            };
        }

        /// <summary>
        /// Türkçe/İngilizce kategori adını normalize et
        /// </summary>
        private static string NormalizeCategory(string? category)
        {
            if (string.IsNullOrEmpty(category))
                return "normal";

            var lower = category.ToLowerInvariant();

            // Türkçe -> İngilizce mapping
            if (lower.Contains("zayıf") || lower.Contains("ince") || lower == "thin")
                return "thin";

            if (lower.Contains("kilolu") || lower.Contains("şişman") || lower == "overweight")
                return "overweight";

            if (lower.Contains("obez") || lower == "obese")
                return "obese";

            // Normal, sağlıklı, orta vs.
            return "normal";
        }

        /// <summary>
        /// Türkçe hedef adını normalize et
        /// </summary>
        private static string NormalizeGoal(string? goal)
        {
            if (string.IsNullOrEmpty(goal))
                return "fit";

            var lower = goal.ToLowerInvariant();

            if (lower.Contains("kas") || lower.Contains("muscle"))
                return "muscle";

            if (lower.Contains("kilo ver") || lower.Contains("zayıfla") || lower.Contains("lean"))
                return "lean";

            if (lower.Contains("sıkılaş") || lower.Contains("tone"))
                return "tone";

            if (lower.Contains("atletik") || lower.Contains("performans") || lower.Contains("athletic"))
                return "athletic";

            // Fit Kalma veya diğer
            return "fit";
        }

        /// <summary>
        /// Hedefe ve mevcut kategoriye göre after görsel tipini belirle
        /// Özel kural: Fit Kalma hedefinde kaynak kategoriye göre farklı after
        /// </summary>
        private static string GetAfterType(string normalizedCategory, string normalizedGoal)
        {
            // Fit Kalma özel kuralı
            if (normalizedGoal == "fit")
            {
                return normalizedCategory switch
                {
                    "thin" => "athletic",      // Zayıf kişi fit olmak istiyorsa atletik göster
                    "normal" => "fit",          // Normal kişi fit kalır
                    "overweight" => "lean",     // Kilolu kişi önce yağ yakar
                    "obese" => "lean",          // Obez kişi önce yağ yakar
                    _ => "fit"
                };
            }

            // Diğer hedefler direkt mapping
            return normalizedGoal;
        }
    }

    /// <summary>
    /// Transformation görsel sonucu
    /// </summary>
    public class TransformationImageResult
    {
        public string BeforePath { get; set; } = string.Empty;
        public string AfterPath { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
    }
}
