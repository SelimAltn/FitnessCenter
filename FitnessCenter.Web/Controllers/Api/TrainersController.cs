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
        /// /api/trainers?date=2025-12-11&serviceId=3&page=1&pageSize=10
        /// Belirtilen tarihin gününe göre (DayOfWeek) müsait eğitmenleri listeler.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TrainerDto>>> GetAvailableTrainers(
            [FromQuery] string? date,
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

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 50 ? 50 : pageSize);

            var gun = targetDate.DayOfWeek;

            // ---- Temel eğitmen sorgusu ----
            IQueryable<Models.Entities.Egitmen> egitmenQuery = _context.Egitmenler.AsNoTracking();

            // Hizmete göre filtre (EgitmenHizmet N-N)
            if (serviceId.HasValue)
            {
                egitmenQuery =
                    from e in _context.Egitmenler
                    join eh in _context.EgitmenHizmetler on e.Id equals eh.EgitmenId
                    where eh.HizmetId == serviceId.Value
                    select e;
            }

            // Musaitlik'e göre filtre
            var availableTrainersQuery =
                from e in egitmenQuery
                join m in _context.Musaitlikler
                    on e.Id equals m.EgitmenId
                where m.Gun == gun
                select new TrainerDto
                {
                    Id = e.Id,
                    AdSoyad = e.AdSoyad,
                    Uzmanlik = e.Uzmanlik
                };

            availableTrainersQuery = availableTrainersQuery.Distinct();

            // ---- Sayfalama ----
            var totalCount = await availableTrainersQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await availableTrainersQuery
                .OrderBy(t => t.AdSoyad)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
