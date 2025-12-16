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

        // Yeni tablolar - Trainer Area
        public DbSet<UzmanlikAlani> UzmanlikAlanlari { get; set; } = null!;
        public DbSet<EgitmenUzmanlik> EgitmenUzmanliklari { get; set; } = null!;
        public DbSet<Mesaj> Mesajlar { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Identity için şart

            // Eğitmen-Hizmet N-N ilişki için birleşik anahtar
            modelBuilder.Entity<EgitmenHizmet>()
                .HasKey(eh => new { eh.EgitmenId, eh.HizmetId });

            // Eğitmen-Uzmanlık N-N ilişki için birleşik anahtar
            modelBuilder.Entity<EgitmenUzmanlik>()
                .HasKey(eu => new { eu.EgitmenId, eu.UzmanlikAlaniId });

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

            // Egitmen - Salon (N - 1) - Tek şube kuralı
            modelBuilder.Entity<Egitmen>()
                .HasOne(e => e.Salon)
                .WithMany()
                .HasForeignKey(e => e.SalonId)
                .OnDelete(DeleteBehavior.SetNull);

            // Egitmen - ApplicationUser (1 - 1)
            modelBuilder.Entity<Egitmen>()
                .HasOne(e => e.ApplicationUser)
                .WithMany()
                .HasForeignKey(e => e.ApplicationUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Mesaj - Gonderen (N - 1)
            modelBuilder.Entity<Mesaj>()
                .HasOne(m => m.Gonderen)
                .WithMany()
                .HasForeignKey(m => m.GonderenId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mesaj - Alici (N - 1)
            modelBuilder.Entity<Mesaj>()
                .HasOne(m => m.Alici)
                .WithMany()
                .HasForeignKey(m => m.AliciId)
                .OnDelete(DeleteBehavior.Restrict);

            // Mesaj - Randevu (N - 1, optional)
            modelBuilder.Entity<Mesaj>()
                .HasOne(m => m.Randevu)
                .WithMany()
                .HasForeignKey(m => m.RandevuId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
