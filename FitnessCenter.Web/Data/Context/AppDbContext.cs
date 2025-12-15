using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore; //DbContext kullanmak için

namespace FitnessCenter.Web.Data.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser> 
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
        public DbSet<Uyelik> Uyelikler { get; set; } = null!;
        public DbSet<SupportTicket> SupportTickets { get; set; } = null!;
        public DbSet<Bildirim> Bildirimler { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity için şart

            // Eğitmen-Hizmet N-N ilişki için birleşik anahtar
            modelBuilder.Entity<EgitmenHizmet>()
                .HasKey(eh => new { eh.EgitmenId, eh.HizmetId });
            // Uye - Uyelik (1 - N)
            modelBuilder.Entity<Uyelik>()
                .HasOne(u => u.Uye)
                .WithMany(u => u.Uyelikler)
                .HasForeignKey(u => u.UyeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Salon - Uyelik (1 - N)
            modelBuilder.Entity<Uyelik>()
                .HasOne(u => u.Salon)
                .WithMany() // istersen Salon tarafına ICollection<Uyelik> ekleyebilirsin
                .HasForeignKey(u => u.SalonId)
                .OnDelete(DeleteBehavior.Cascade);

            // Uye - ApplicationUser (opsiyonel 1 - 1 veya 1 - N gibi)
            modelBuilder.Entity<Uye>()
                .HasOne(u => u.ApplicationUser)
                .WithMany() // İstersen ApplicationUser içine ICollection<Uye> ekleyebilirsin
                .HasForeignKey(u => u.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
