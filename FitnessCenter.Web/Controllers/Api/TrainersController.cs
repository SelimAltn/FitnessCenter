using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace FitnessCenter.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrainersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TrainersController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Eğitmenleri listeler. Parametreler opsiyoneldir.
        /// 
        /// - date boş: Tüm aktif eğitmenleri döner
        /// - date dolu: Belirtilen tarihin gününe göre müsait eğitmenleri döner
        /// - salonId: Şubeye göre filtreler (date doluysa zorunlu, boşsa opsiyonel)
        /// - serviceId: Hizmete göre filtreler
        /// 
        /// Örnek kullanımlar:
        /// - GET /api/trainers → Tüm eğitmenler
        /// - GET /api/trainers?salonId=1 → 1 nolu şubedeki eğitmenler
        /// - GET /api/trainers?date=2025-12-11&amp;salonId=1 → Belirtilen tarihte müsait eğitmenler
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TrainerDto>>> GetAvailableTrainers(
            [FromQuery] string? date = null,
            [FromQuery] int? salonId = null,
            [FromQuery] int? serviceId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            DateTime? targetDate = null;

            // ---- Date parametresi varsa parse et ----
            if (!string.IsNullOrWhiteSpace(date))
            {
                if (!DateTime.TryParse(date, out var parsedDate))
                {
                    return Problem(
                        statusCode: 400,
                        title: "Geçersiz tarih formatı",
                        detail: "date parametresi geçerli bir tarih olmalıdır (yyyy-MM-dd).",
                        type: "https://fitnesscenter.com/probs/invalid-date");
                }
                targetDate = parsedDate;

                // Date verilmişse salonId de zorunlu
                if (!salonId.HasValue)
                {
                    return Problem(
                        statusCode: 400,
                        title: "Geçersiz parametre",
                        detail: "date parametresi kullanılırken salonId zorunludur.",
                        type: "https://fitnesscenter.com/probs/invalid-parameter");
                }
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 50 ? 50 : pageSize);

            // ---- Temel eğitmen sorgusu: Aktif olanlar ----
            IQueryable<Models.Entities.Egitmen> egitmenQuery = _context.Egitmenler
                .Where(e => e.Aktif)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .AsNoTracking();

            // Şubeye göre filtre (opsiyonel)
            if (salonId.HasValue)
            {
                egitmenQuery = egitmenQuery.Where(e => e.SalonId == salonId.Value);
            }

            // Hizmete göre filtre (EgitmenHizmet N-N)
            if (serviceId.HasValue)
            {
                egitmenQuery = egitmenQuery.Where(e => 
                    e.EgitmenHizmetler!.Any(eh => eh.HizmetId == serviceId.Value));
            }

            // Tarihe göre müsaitlik filtresi (sadece date verilmişse)
            IQueryable<Models.Entities.Egitmen> filteredQuery;
            if (targetDate.HasValue)
            {
                var gun = targetDate.Value.DayOfWeek;
                // Müsaitlik kaydı yoksa yine de göster (henüz program belirlenmemiş olabilir)
                filteredQuery = egitmenQuery.Where(e => 
                    e.Musaitlikler == null || 
                    !e.Musaitlikler.Any() || 
                    e.Musaitlikler.Any(m => m.Gun == gun));
            }
            else
            {
                // Date yoksa tüm eğitmenleri göster
                filteredQuery = egitmenQuery;
            }

            var items = await filteredQuery
                .OrderBy(e => e.AdSoyad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new TrainerDto
                {
                    Id = e.Id,
                    AdSoyad = e.AdSoyad,
                    Uzmanlik = e.EgitmenUzmanliklari != null 
                        ? string.Join(", ", e.EgitmenUzmanliklari.Select(eu => eu.UzmanlikAlani!.Ad))
                        : ""
                })
                .ToListAsync();

            var totalCount = await filteredQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = new PagedResult<TrainerDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };

            return Ok(result);
        }
    }
}

