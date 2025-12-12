using System;
using System.Linq;
using System.Threading.Tasks;
using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FitnessCenter.Web.Models.Entities;

namespace FitnessCenter.Web.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Admin + Member

    public class MembersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MembersController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;

        }

        /// <summary>
        /// /api/members/5/appointments?from=2025-12-01&to=2025-12-31&status=Onaylandı&page=1&pageSize=10
        /// Belirli bir üyenin randevularını döner.
        /// </summary>
        [HttpGet("{id:int}/appointments")]
        public async Task<ActionResult<PagedResult<AppointmentDto>>> GetMemberAppointments(
            int id,
            [FromQuery] string? from,
            [FromQuery] string? to,
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // ---- Kullanıcı bu veriyi görmeye yetkili mi? ----
            // Admin ise herkesin randevusunu görebilir
            if (!User.IsInRole("Admin"))
            {
                // Giriş yapmış Identity kullanıcısının Id'si
                var currentUserId = _userManager.GetUserId(User);

                // Bu Identity user'a bağlı Uye kaydı
                var currentUye = await _context.Uyeler
                    .AsNoTracking()
                    .SingleOrDefaultAsync(u => u.ApplicationUserId == currentUserId);

                // Uye bulunamadıysa veya istenen id kendisine ait değilse → 403
                if (currentUye == null || currentUye.Id != id)
                {
                    return Forbid(); // 403 Forbidden
                }
            }

            // ---- Üye var mı? ----
            var uyeExists = await _context.Uyeler.AnyAsync(u => u.Id == id);
            if (!uyeExists)
            {
                return Problem(
                    statusCode: 404,
                    title: "Üye bulunamadı",
                    detail: $"Belirtilen ID'ye sahip üye bulunamadı: {id}",
                    type: "https://fitnesscenter.com/probs/member-not-found");
            }

            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 10 : (pageSize > 50 ? 50 : pageSize);

            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(from))
            {
                if (!DateTime.TryParse(from, out var tmpFrom))
                {
                    return Problem(
                        statusCode: 400,
                        title: "Geçersiz tarih",
                        detail: "from parametresi geçerli bir tarih olmalıdır (yyyy-MM-dd).",
                        type: "https://fitnesscenter.com/probs/invalid-date");
                }
                fromDate = tmpFrom.Date;
            }

            if (!string.IsNullOrWhiteSpace(to))
            {
                if (!DateTime.TryParse(to, out var tmpTo))
                {
                    return Problem(
                        statusCode: 400,
                        title: "Geçersiz tarih",
                        detail: "to parametresi geçerli bir tarih olmalıdır (yyyy-MM-dd).",
                        type: "https://fitnesscenter.com/probs/invalid-date");
                }
                toDate = tmpTo.Date;
            }

            // ---- Temel randevu sorgusu ----
            var query =
                from r in _context.Randevular
                join h in _context.Hizmetler on r.HizmetId equals h.Id
                join e in _context.Egitmenler on r.EgitmenId equals e.Id
                join s in _context.Salonlar on r.SalonId equals s.Id
                where r.UyeId == id
                select new
                {
                    r.Id,
                    r.BaslangicZamani,
                    r.BitisZamani,
                    r.Durum,
                    HizmetAdi = h.Ad,
                    EgitmenAdSoyad = e.AdSoyad,
                    SalonAdi = s.Ad
                };

            // Tarih aralığı
            if (fromDate.HasValue)
            {
                query = query.Where(x => x.BaslangicZamani.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(x => x.BaslangicZamani.Date <= toDate.Value);
            }

            // Durum filtresi
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Durum == status);
            }

            // ---- Sayfalama ----
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .OrderByDescending(x => x.BaslangicZamani)
                .ThenBy(x => x.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AppointmentDto
                {
                    Id = x.Id,
                    BaslangicZamani = x.BaslangicZamani,
                    BitisZamani = x.BitisZamani,
                    HizmetAdi = x.HizmetAdi,
                    EgitmenAdSoyad = x.EgitmenAdSoyad,
                    SalonAdi = x.SalonAdi,
                    Durum = x.Durum
                })
                .ToListAsync();

            var result = new PagedResult<AppointmentDto>
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
