using System.ComponentModel.DataAnnotations;

namespace FitnessCenter.Web.Models.Entities
{
    public class Salon : IValidatableObject
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Salon adı zorunludur.")]
        [StringLength(100, ErrorMessage = "Salon adı en fazla 100 karakter olabilir.")]
        [Display(Name = "Salon Adı")]
        public string Ad { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
        [Display(Name = "Adres")]
        public string? Adress { get; set; }

        [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }

        #region Çalışma Saatleri

        /// <summary>
        /// 7/24 açık salon mu? True ise AcilisSaati/KapanisSaati dikkate alınmaz.
        /// </summary>
        [Display(Name = "7/24 Açık")]
        public bool Is24Hours { get; set; } = false;

        /// <summary>
        /// Salon açılış saati (Is24Hours=false ise zorunlu)
        /// </summary>
        [Display(Name = "Açılış Saati")]
        [DataType(DataType.Time)]
        public TimeSpan? AcilisSaati { get; set; }

        /// <summary>
        /// Salon kapanış saati (Is24Hours=false ise zorunlu)
        /// </summary>
        [Display(Name = "Kapanış Saati")]
        [DataType(DataType.Time)]
        public TimeSpan? KapanisSaati { get; set; }

        #endregion

        public ICollection<Randevu>? Randevular { get; set; }

        /// <summary>
        /// Is24Hours=false iken AcilisSaati ve KapanisSaati zorunlu ve AcilisSaati < KapanisSaati olmalı
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Is24Hours)
            {
                if (!AcilisSaati.HasValue)
                {
                    yield return new ValidationResult(
                        "7/24 açık olmayan salonlar için açılış saati zorunludur.",
                        new[] { nameof(AcilisSaati) });
                }

                if (!KapanisSaati.HasValue)
                {
                    yield return new ValidationResult(
                        "7/24 açık olmayan salonlar için kapanış saati zorunludur.",
                        new[] { nameof(KapanisSaati) });
                }

                if (AcilisSaati.HasValue && KapanisSaati.HasValue)
                {
                    if (AcilisSaati.Value >= KapanisSaati.Value)
                    {
                        yield return new ValidationResult(
                            "Açılış saati kapanış saatinden önce olmalıdır.",
                            new[] { nameof(AcilisSaati), nameof(KapanisSaati) });
                    }
                }
            }
        }
    }
}
