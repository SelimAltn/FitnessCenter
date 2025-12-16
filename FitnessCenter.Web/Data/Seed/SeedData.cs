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
            string[] roles = new[] { "Admin", "Member", "Trainer" };

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

            // 3.0 Uzmanlık Alanları seed
            if (!await context.UzmanlikAlanlari.AnyAsync())
            {
                context.UzmanlikAlanlari.AddRange(
                    new UzmanlikAlani { Ad = "Fitness", Aciklama = "Genel fitness ve kondisyon çalışmaları", Aktif = true },
                    new UzmanlikAlani { Ad = "Yoga", Aciklama = "Yoga ve meditasyon seansları", Aktif = true },
                    new UzmanlikAlani { Ad = "Pilates", Aciklama = "Pilates egzersizleri", Aktif = true },
                    new UzmanlikAlani { Ad = "CrossFit", Aciklama = "Yüksek yoğunluklu fonksiyonel antrenman", Aktif = true },
                    new UzmanlikAlani { Ad = "Rehabilitasyon", Aciklama = "Spor sakatlıkları rehabilitasyonu", Aktif = true },
                    new UzmanlikAlani { Ad = "Beslenme", Aciklama = "Beslenme danışmanlığı", Aktif = true },
                    new UzmanlikAlani { Ad = "Kişisel Antrenman", Aciklama = "Bire bir kişisel antrenman programları", Aktif = true },
                    new UzmanlikAlani { Ad = "Grup Dersleri", Aciklama = "Grup fitness dersleri", Aktif = true },
                    new UzmanlikAlani { Ad = "HIIT", Aciklama = "High Intensity Interval Training", Aktif = true },
                    new UzmanlikAlani { Ad = "Masaj", Aciklama = "Sporcu masajı ve gevşeme teknikleri", Aktif = true }
                );

                await context.SaveChangesAsync();
            }

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

            // 3.3 Eğitmen seed (artık Identity olmadan, admin sonradan ekleyecek)
            if (!await context.Egitmenler.AnyAsync())
            {
                var merkezSube = await context.Salonlar.FirstOrDefaultAsync(s => s.Ad == "Merkez Şube");
                var premiumSube = await context.Salonlar.FirstOrDefaultAsync(s => s.Ad == "Premium Şube");

                context.Egitmenler.AddRange(
                    new Egitmen
                    {
                        AdSoyad = "Ahmet Yılmaz",
                        Email = "ahmet@fitnesscenter.com",
                        Telefon = "0532 111 2233",
                        SalonId = merkezSube?.Id,
                        Biyografi = "10 yıllık deneyime sahip sertifikalı personal trainer.",
                        Aktif = true
                    },
                    new Egitmen
                    {
                        AdSoyad = "Ayşe Demir",
                        Email = "ayse@fitnesscenter.com",
                        Telefon = "0532 444 5566",
                        SalonId = merkezSube?.Id,
                        Biyografi = "Enerjik grup dersleri ve pilates eğitmeni.",
                        Aktif = true
                    },
                    new Egitmen
                    {
                        AdSoyad = "Mehmet Kaya",
                        Email = "mehmet@fitnesscenter.com",
                        Telefon = "0532 777 8899",
                        SalonId = premiumSube?.Id,
                        Biyografi = "Fizyoterapi kökenli sporcu masajı uzmanı.",
                        Aktif = true
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
