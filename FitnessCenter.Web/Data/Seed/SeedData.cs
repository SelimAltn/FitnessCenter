using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using FitnessCenter.Web.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Data.Seed
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // ---- 1) ROLLER ----
            string[] roles = new[] { "Admin", "Member" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ---- 2) ADMIN KULLANICI ----
            var adminEmail = "admin@fitnesscenter.com";
            var adminUserName = "admin";
            var adminPassword = "Admin123!";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // ---- 3) ÖRNEK SALON / HİZMET / EĞİTMEN ----

            // Veritabanı gerçekten hazır mı emin olmak için
            await context.Database.EnsureCreatedAsync();

            // 3.1 Salon seed
            if (!await context.Salonlar.AnyAsync())
            {
                context.Salonlar.AddRange(
                    new Salon
                    {
                        Ad = "Merkez Şube",
                        Adress = "Sakarya / Serdivan",
                        Aciklama = "Ana fitness salonu. Kardiyo, ağırlık ve ders stüdyosu."
                    },
                    new Salon
                    {
                        Ad = "Premium Şube",
                        Adress = "İstanbul / Kadıköy",
                        Aciklama = "Daha butik, randevulu kişisel antrenman salonu."
                    }
                );

                await context.SaveChangesAsync();
            }

            // 3.2 Hizmet seed
            if (!await context.Hizmetler.AnyAsync())
            {
                context.Hizmetler.AddRange(
                    new Hizmet
                    {
                        Ad = "Kişisel Antrenman",
                        SureDakika = 60,
                        Ucret = 500m,
                        Aciklama = "Bire bir antrenör eşliğinde kişisel program."
                    },
                    new Hizmet
                    {
                        Ad = "Grup Fitness Dersi",
                        SureDakika = 45,
                        Ucret = 250m,
                        Aciklama = "Maksimum 10 kişilik grup dersleri."
                    },
                    new Hizmet
                    {
                        Ad = "Sporcu Masajı",
                        SureDakika = 50,
                        Ucret = 450m,
                        Aciklama = "Antrenman sonrası kas gevşetici masaj."
                    }
                );

                await context.SaveChangesAsync();
            }

            // 3.3 Eğitmen seed
            if (!await context.Egitmenler.AnyAsync())
            {
                context.Egitmenler.AddRange(
                    new Egitmen
                    {
                        AdSoyad = "Ahmet Yılmaz",
                        Uzmanlik = "Kişisel Antrenman, Fonksiyonel Antrenman",
                        Biyografi = "10 yıllık deneyime sahip sertifikalı personal trainer."
                    },
                    new Egitmen
                    {
                        AdSoyad = "Ayşe Demir",
                        Uzmanlik = "Grup Dersleri, HIIT, Pilates",
                        Biyografi = "Enerjik grup dersleri ve pilates eğitmeni."
                    },
                    new Egitmen
                    {
                        AdSoyad = "Mehmet Kaya",
                        Uzmanlik = "Masaj, Rehabilitasyon",
                        Biyografi = "Fizyoterapi kökenli sporcu masajı uzmanı."
                    }
                );

                await context.SaveChangesAsync();
            }

            // 3.4 Eğitmen-Hizmet ilişki seed (N-N tablo)
            if (!await context.EgitmenHizmetler.AnyAsync())
            {
                var pt = await context.Hizmetler.FirstOrDefaultAsync(h => h.Ad == "Kişisel Antrenman");
                var grup = await context.Hizmetler.FirstOrDefaultAsync(h => h.Ad == "Grup Fitness Dersi");
                var masaj = await context.Hizmetler.FirstOrDefaultAsync(h => h.Ad == "Sporcu Masajı");

                var ahmet = await context.Egitmenler.FirstOrDefaultAsync(e => e.AdSoyad == "Ahmet Yılmaz");
                var ayse = await context.Egitmenler.FirstOrDefaultAsync(e => e.AdSoyad == "Ayşe Demir");
                var mehmet = await context.Egitmenler.FirstOrDefaultAsync(e => e.AdSoyad == "Mehmet Kaya");

                if (pt != null && grup != null && masaj != null &&
                    ahmet != null && ayse != null && mehmet != null)
                {
                    context.EgitmenHizmetler.AddRange(
                        new EgitmenHizmet { EgitmenId = ahmet.Id, HizmetId = pt.Id },
                        new EgitmenHizmet { EgitmenId = ayse.Id, HizmetId = grup.Id },
                        new EgitmenHizmet { EgitmenId = mehmet.Id, HizmetId = masaj.Id },

                        // İlave kombinasyonlar:
                        new EgitmenHizmet { EgitmenId = ahmet.Id, HizmetId = grup.Id },
                        new EgitmenHizmet { EgitmenId = ayse.Id, HizmetId = pt.Id }
                    );

                    await context.SaveChangesAsync();
                }
            }

        }
    }
}
