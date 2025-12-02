using FitnessCenter.Web.Models.Entities;
using Microsoft.EntityFrameworkCore; //DbContext kullanmak için

namespace FitnessCenter.Web.Data.Context
{
    public class AppDbContext : DbContext // --> DbContext = hazır, EF Core’un kendi sınıfı
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // Tablo setleri
        public DbSet<Salon> Salonlar { get; set; }
        public DbSet<Hizmet> Hizmetler { get; set; }
        public DbSet<Egitmen> Egitmenler { get; set; }
        public DbSet<EgitmenHizmet> EgitmenHizmetler { get; set; }
        public DbSet<Uye> Uyeler { get; set; }
        public DbSet<Randevu> Randevular { get; set; }
        public DbSet<Musaitlik> Musaitlikler { get; set; }
        public DbSet<AiLog> AiLoglar { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Eğitmen-Hizmet N-N ilişki için birleşik anahtar
            modelBuilder.Entity<EgitmenHizmet>()
                .HasKey(eh => new { eh.EgitmenId, eh.HizmetId });
        }
    }
}
