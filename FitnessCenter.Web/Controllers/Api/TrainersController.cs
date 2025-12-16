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
        /// /api/trainers?date=2025-12-11&salonId=1&serviceId=3&page=1&pageSize=10
        /// Belirtilen tarihin gününe ve şubeye göre müsait eğitmenleri listeler.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TrainerDto>>> GetAvailableTrainers(
            [FromQuery] string? date,
            [FromQuery] int? salonId,
            [FromQuery] int? serviceId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // ---- Parametre kontrolleri (ProblemDetails) ----
            if (string.IsNullOrWhiteSpace(date))
            {
                return Problem(
                    statusCode: 400,
                    title: "Geçersiz parametre",
                    detail: "date parametresi zorunludur. Örn: ?date=2025-12-11",
                    type: "https://fitnesscenter.com/probs/invalid-parameter");
            }

            if (!DateTime.TryParse(date, out var targetDate))
            {
                return Problem(
                    statusCode: 400,
                    title: "Geçersiz tarih formatı",
                    detail: "date parametresi geçerli bir tarih olmalıdır (yyyy-MM-dd).",
                    type: "https://fitnesscenter.com/probs/invalid-date");
            }

            if (!salonId.HasValue)
            {
                return Problem(
                    statusCode: 400,
                    title: "Geçersiz parametre",
                    detail: "salonId parametresi zorunludur.",
                    type: "https://fitnesscenter.com/probs/invalid-parameter");
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 50 ? 50 : pageSize);

            var gun = targetDate.DayOfWeek;

            // ---- Temel eğitmen sorgusu: Aktif VE bu şubede çalışan ----
            IQueryable<Models.Entities.Egitmen> egitmenQuery = _context.Egitmenler
                .Where(e => e.Aktif && e.SalonId == salonId.Value)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .AsNoTracking();

            // Hizmete göre filtre (EgitmenHizmet N-N)
            if (serviceId.HasValue)
            {
                egitmenQuery = egitmenQuery.Where(e => 
                    e.EgitmenHizmetler!.Any(eh => eh.HizmetId == serviceId.Value));
            }

            // Musaitlik'e göre filtre - eğer müsaitlik kaydı varsa o günde çalışanları göster
            // Müsaitlik kaydı yoksa yine de göster (henüz program belirlenmemiş olabilir)
            var filteredQuery = egitmenQuery.Where(e => 
                e.Musaitlikler == null || 
                !e.Musaitlikler.Any() || 
                e.Musaitlikler.Any(m => m.Gun == gun));

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
