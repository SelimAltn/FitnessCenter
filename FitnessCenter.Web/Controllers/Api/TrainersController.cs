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
        
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PagedResult<TrainerDto>>> GetAvailableTrainers(
            [FromQuery] int? salonId = null,
            [FromQuery] int? hizmetId = null,
            [FromQuery] string? start = null,
            [FromQuery] int? excludeRandevuId = null,  // Edit modunda mevcut randevuyu hariç tut
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            // ---- Tüm parametreler zorunlu ----
            if (!salonId.HasValue || !hizmetId.HasValue || string.IsNullOrWhiteSpace(start))
            {
                // 3 alan seçilmemişse boş liste dön
                return Ok(new PagedResult<TrainerDto>
                {
                    Items = new List<TrainerDto>(),
                    Page = 1,
                    PageSize = pageSize,
                    TotalCount = 0,
                    TotalPages = 0
                });
            }

            // ---- Start parametresini parse et ----
            if (!DateTime.TryParse(start, out var startDateTime))
            {
                return Problem(
                    statusCode: 400,
                    title: "Geçersiz tarih formatı",
                    detail: "start parametresi geçerli bir tarih/saat olmalıdır.",
                    type: "https://fitnesscenter.com/probs/invalid-date");
            }

            // ---- Hizmet süresini al ----
            var hizmet = await _context.Hizmetler.FindAsync(hizmetId.Value);
            if (hizmet == null)
            {
                return Problem(
                    statusCode: 400,
                    title: "Hizmet bulunamadı",
                    detail: "Seçilen hizmet sistemde bulunamadı.",
                    type: "https://fitnesscenter.com/probs/service-not-found");
            }

            var endDateTime = startDateTime.AddMinutes(hizmet.SureDakika);
            var gun = startDateTime.DayOfWeek;
            var startTime = startDateTime.TimeOfDay;
            var endTime = endDateTime.TimeOfDay;

            // ---- Koşul A: Şubede çalışmalı ----
            var query = _context.Egitmenler
                .Where(e => e.Aktif && e.SalonId == salonId.Value)
                .Include(e => e.EgitmenHizmetler)
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
                .Include(e => e.Musaitlikler)
                .AsNoTracking();

            // ---- Koşul B: Hizmeti verebilmeli ----
            query = query.Where(e => 
                e.EgitmenHizmetler != null && 
                e.EgitmenHizmetler.Any(eh => eh.HizmetId == hizmetId.Value));

            // ---- Koşul C: Seçilen tarih/saatte müsait olmalı ----
            // Müsaitlik kaydı olmalı ve saat aralığı uygun olmalı
            query = query.Where(e =>
                e.Musaitlikler != null &&
                e.Musaitlikler.Any(m =>
                    m.Gun == gun &&
                    m.BaslangicSaati <= startTime &&
                    m.BitisSaati >= endTime));

            // Önce uygun eğitmenlerin ID'lerini al
            var uygunEgitmenIdler = await query.Select(e => e.Id).ToListAsync();

            // ---- Koşul D: Çakışan randevusu olmamalı ----
            // Aynı gündeki randevuları kontrol et (Edit modunda mevcut randevuyu hariç tut)
            var conflictQuery = _context.Randevular
                .Where(r =>
                    uygunEgitmenIdler.Contains(r.EgitmenId) &&
                    r.BaslangicZamani.Date == startDateTime.Date &&
                    r.Durum != "İptal" &&
                    // Çakışma kontrolü: existingStart < newEnd AND existingEnd > newStart
                    r.BaslangicZamani < endDateTime &&
                    r.BitisZamani > startDateTime);

            // Edit modunda mevcut randevuyu çakışma kontrolünden hariç tut
            if (excludeRandevuId.HasValue)
            {
                conflictQuery = conflictQuery.Where(r => r.Id != excludeRandevuId.Value);
            }

            var cakisanRandevuEgitmenIdler = await conflictQuery
                .Select(r => r.EgitmenId)
                .Distinct()
                .ToListAsync();

            // Çakışan eğitmenleri çıkar
            var finalEgitmenIdler = uygunEgitmenIdler
                .Where(id => !cakisanRandevuEgitmenIdler.Contains(id))
                .ToList();

            // Final listeyi al
            var items = await _context.Egitmenler
                .Where(e => finalEgitmenIdler.Contains(e.Id)) // filtereme                
            
                .Include(e => e.EgitmenUzmanliklari!)
                    .ThenInclude(eu => eu.UzmanlikAlani)
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

            var totalCount = finalEgitmenIdler.Count;
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

