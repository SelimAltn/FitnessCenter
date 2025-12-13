using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitnessCenter.Web.Models.Entities
{
    public class AiLog
    {
        public int Id { get; set; }

        [Display(Name = "Üye")]
        public int? UyeId { get; set; }

        [Required(ErrorMessage = "Soru metni zorunludur.")]
        [StringLength(2000, ErrorMessage = "Soru en fazla 2000 karakter olabilir.")]
        [Display(Name = "Soru Metni")]
        public string SoruMetni { get; set; } = null!;

        [Required(ErrorMessage = "Cevap metni zorunludur.")]
        [Display(Name = "Cevap Metni")]
        public string CevapMetni { get; set; } = null!;

        [DataType(DataType.DateTime)]
        [Display(Name = "Oluşturulma Zamanı")]
        public DateTime OlusturulmaZamani { get; set; } = DateTime.UtcNow;

        // ===== Cache ve Log için Yeni Alanlar =====

        /// <summary>
        /// Input verilerinin SHA256 hash'i (cache key olarak kullanılır)
        /// </summary>
        [StringLength(64)]
        [Display(Name = "Input Hash")]
        public string? InputHash { get; set; }

        /// <summary>
        /// Sonuç cache'den mi geldi?
        /// </summary>
        [Display(Name = "Cache'den Geldi")]
        public bool IsCached { get; set; }

        /// <summary>
        /// AI'dan dönen ham JSON yanıt (nvarchar(max))
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        [Display(Name = "JSON Yanıt")]
        public string? ResponseJson { get; set; }

        /// <summary>
        /// Hata durumunda mesaj
        /// </summary>
        [StringLength(1000)]
        [Display(Name = "Hata Mesajı")]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Kullanılan AI model adı
        /// </summary>
        [StringLength(100)]
        [Display(Name = "Model Adı")]
        public string? ModelName { get; set; }

        /// <summary>
        /// API çağrı süresi (milisaniye)
        /// </summary>
        [Display(Name = "Süre (ms)")]
        public int? DurationMs { get; set; }

        /// <summary>
        /// İşlem başarılı mı?
        /// </summary>
        [Display(Name = "Başarılı")]
        public bool IsSuccess { get; set; }

        // ===== Navigation Property =====

        [Display(Name = "Üye")]
        public Uye? Uye { get; set; }
    }
}
