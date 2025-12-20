using FitnessCenter.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using FitnessCenter.Web.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace FitnessCenter.Web.Data.Seed
{
    /// <summary>
    /// Complete database reset and seed utility.
    /// Preserves: g231210558@sakarya.edu.tr (admin), s.la55m (member with AiLogs)
    /// All text is ASCII-only.
    /// </summary>
    public static class MasterSeedData
    {
        // Protected users
        private const string AdminUsername = "g231210558@sakarya.edu.tr";
        private const string ProtectedMemberUsername = "s.la55m";
        private const string DefaultPassword = "123456";
        private const decimal YearlyMembershipFee = 24000m;

        public static async Task RunAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            Console.WriteLine("=== MASTER SEED STARTING ===");

            // =============================================
            // STEP 1: Find protected users
            // =============================================
            var adminUser = await userManager.FindByNameAsync(AdminUsername);
            var sla55mUser = await userManager.FindByNameAsync(ProtectedMemberUsername) 
                          ?? await userManager.FindByEmailAsync(ProtectedMemberUsername);

            string? adminId = adminUser?.Id;
            string? sla55mId = sla55mUser?.Id;
            int? sla55mUyeId = null;

            if (sla55mId != null)
            {
                var sla55mUye = await context.Uyeler.FirstOrDefaultAsync(u => u.ApplicationUserId == sla55mId);
                sla55mUyeId = sla55mUye?.Id;
            }

            Console.WriteLine($"Admin ID: {adminId ?? "NOT FOUND"}");
            Console.WriteLine($"s.la55m ID: {sla55mId ?? "NOT FOUND"}, UyeId: {sla55mUyeId?.ToString() ?? "NOT FOUND"}");

            // =============================================
            // STEP 2: Clean s.la55m's Randevular and Uyelikler only
            // =============================================
            if (sla55mUyeId.HasValue)
            {
                var sla55mRandevular = await context.Randevular.Where(r => r.UyeId == sla55mUyeId.Value).ToListAsync();
                context.Randevular.RemoveRange(sla55mRandevular);

                var sla55mUyelikler = await context.Uyelikler.Where(u => u.UyeId == sla55mUyeId.Value).ToListAsync();
                context.Uyelikler.RemoveRange(sla55mUyelikler);

                await context.SaveChangesAsync();
                Console.WriteLine("Cleaned s.la55m's Randevular and Uyelikler (AiLogs preserved)");
            }

            // =============================================
            // STEP 3: General cleanup (all domain data)
            // =============================================
            
            // 3.1 All Randevular
            context.Randevular.RemoveRange(context.Randevular);
            await context.SaveChangesAsync();

            // 3.2 Mesajlar
            context.Mesajlar.RemoveRange(context.Mesajlar);
            await context.SaveChangesAsync();

            // 3.3 Musaitlikler
            context.Musaitlikler.RemoveRange(context.Musaitlikler);
            await context.SaveChangesAsync();

            // 3.4 EgitmenHizmetler
            context.EgitmenHizmetler.RemoveRange(context.EgitmenHizmetler);
            await context.SaveChangesAsync();

            // 3.5 EgitmenUzmanliklari
            context.EgitmenUzmanliklari.RemoveRange(context.EgitmenUzmanliklari);
            await context.SaveChangesAsync();

            // 3.6 Egitmenler
            context.Egitmenler.RemoveRange(context.Egitmenler);
            await context.SaveChangesAsync();

            // 3.7 Hizmetler
            context.Hizmetler.RemoveRange(context.Hizmetler);
            await context.SaveChangesAsync();

            // 3.8 UzmanlikAlanlari
            context.UzmanlikAlanlari.RemoveRange(context.UzmanlikAlanlari);
            await context.SaveChangesAsync();

            // 3.9 Uyelikler
            context.Uyelikler.RemoveRange(context.Uyelikler);
            await context.SaveChangesAsync();

            // 3.10 Salonlar
            context.Salonlar.RemoveRange(context.Salonlar);
            await context.SaveChangesAsync();

            // 3.11 AiLoglar - preserve s.la55m's
            if (sla55mUyeId.HasValue)
            {
                var logsToDelete = await context.AiLoglar.Where(a => a.UyeId != sla55mUyeId.Value).ToListAsync();
                context.AiLoglar.RemoveRange(logsToDelete);
            }
            else
            {
                context.AiLoglar.RemoveRange(context.AiLoglar);
            }
            await context.SaveChangesAsync();

            // 3.12 Bildirimler - preserve for admin/s.la55m
            var protectedUserIds = new List<string>();
            if (adminId != null) protectedUserIds.Add(adminId);
            if (sla55mId != null) protectedUserIds.Add(sla55mId);

            var bildirimsToDelete = await context.Bildirimler
                .Where(b => b.UserId == null || !protectedUserIds.Contains(b.UserId))
                .ToListAsync();
            context.Bildirimler.RemoveRange(bildirimsToDelete);
            await context.SaveChangesAsync();

            // 3.13 SupportTickets
            var ticketsToDelete = await context.SupportTickets
                .Where(t => t.UserId == null || !protectedUserIds.Contains(t.UserId))
                .ToListAsync();
            context.SupportTickets.RemoveRange(ticketsToDelete);
            await context.SaveChangesAsync();

            Console.WriteLine("Domain data cleaned");

            // =============================================
            // STEP 4: Clean Identity users except protected
            // =============================================
            var allUsers = await userManager.Users.ToListAsync();
            foreach (var user in allUsers)
            {
                if (user.Id == adminId || user.Id == sla55mId)
                    continue;

                // Delete related Uye record first
                var uyeRecord = await context.Uyeler.FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);
                if (uyeRecord != null)
                {
                    context.Uyeler.Remove(uyeRecord);
                    await context.SaveChangesAsync();
                }

                await userManager.DeleteAsync(user);
            }
            Console.WriteLine("Identity users cleaned");

            // =============================================
            // STEP 5: Ensure roles exist
            // =============================================
            string[] roles = { "Admin", "Member", "Trainer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // =============================================
            // STEP 6: Seed 20 Salonlar
            // =============================================
            var salonlar = new List<Salon>
            {
                // 2 Merkez (7/24)
                new Salon { Ad = "Merkez Sube 1", Adress = "Istanbul Kadikoy Merkez", Aciklama = "Ana merkez subesi, 7/24 acik", Is24Hours = true },
                new Salon { Ad = "Merkez Sube 2", Adress = "Ankara Kizilay Merkez", Aciklama = "Baskent merkez subesi, 7/24 acik", Is24Hours = true },
                // 18 Standard (06:00-00:00)
                new Salon { Ad = "Sube Serdivan", Adress = "Sakarya Serdivan", Aciklama = "Serdivan subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Adapazari", Adress = "Sakarya Adapazari", Aciklama = "Adapazari subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Besiktas", Adress = "Istanbul Besiktas", Aciklama = "Besiktas subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Uskudar", Adress = "Istanbul Uskudar", Aciklama = "Uskudar subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Sisli", Adress = "Istanbul Sisli", Aciklama = "Sisli subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Bakirkoy", Adress = "Istanbul Bakirkoy", Aciklama = "Bakirkoy subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Pendik", Adress = "Istanbul Pendik", Aciklama = "Pendik subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Kartal", Adress = "Istanbul Kartal", Aciklama = "Kartal subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Cankaya", Adress = "Ankara Cankaya", Aciklama = "Cankaya subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Mamak", Adress = "Ankara Mamak", Aciklama = "Mamak subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Bornova", Adress = "Izmir Bornova", Aciklama = "Bornova subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Karsiyaka", Adress = "Izmir Karsiyaka", Aciklama = "Karsiyaka subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Nilufer", Adress = "Bursa Nilufer", Aciklama = "Nilufer subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Osmangazi", Adress = "Bursa Osmangazi", Aciklama = "Osmangazi subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Seyhan", Adress = "Adana Seyhan", Aciklama = "Seyhan subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Kepez", Adress = "Antalya Kepez", Aciklama = "Kepez subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Meram", Adress = "Konya Meram", Aciklama = "Meram subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) },
                new Salon { Ad = "Sube Atakum", Adress = "Samsun Atakum", Aciklama = "Atakum subesi", Is24Hours = false, AcilisSaati = new TimeSpan(6, 0, 0), KapanisSaati = new TimeSpan(0, 0, 0) }
            };
            context.Salonlar.AddRange(salonlar);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {salonlar.Count} Salonlar");

            // =============================================
            // STEP 7: Seed Uzmanlik Alanlari
            // =============================================
            var uzmanliklar = new List<UzmanlikAlani>
            {
                new UzmanlikAlani { Ad = "kilo_verme", Aciklama = "Kilo verme ve yag yakimi programlari", Aktif = true },
                new UzmanlikAlani { Ad = "kas_gelistirme", Aciklama = "Kas kutlesi artirma ve vucut gelistirme", Aktif = true },
                new UzmanlikAlani { Ad = "yoga", Aciklama = "Yoga ve nefes teknikleri", Aktif = true },
                new UzmanlikAlani { Ad = "pilates", Aciklama = "Pilates egzersizleri", Aktif = true },
                new UzmanlikAlani { Ad = "fonksiyonel", Aciklama = "Fonksiyonel antrenman", Aktif = true },
                new UzmanlikAlani { Ad = "kardiyo", Aciklama = "Kardiyovaskuler dayaniklilik", Aktif = true },
                new UzmanlikAlani { Ad = "dovus_sporu", Aciklama = "Kickbox, boks ve dovus sporlari", Aktif = true },
                new UzmanlikAlani { Ad = "postur_mobilite", Aciklama = "Postur duzeltme ve esneklik", Aktif = true }
            };
            context.UzmanlikAlanlari.AddRange(uzmanliklar);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {uzmanliklar.Count} UzmanlikAlanlari");

            // =============================================
            // STEP 8: Seed Hizmetler
            // =============================================
            var hizmetler = new List<Hizmet>
            {
                new Hizmet { Ad = "fitness", SureDakika = 60, Aciklama = "Genel fitness antrenmani" },
                new Hizmet { Ad = "personal_training", SureDakika = 60, Aciklama = "Bire bir kisisel antrenman" },
                new Hizmet { Ad = "hiit", SureDakika = 30, Aciklama = "Yuksek yogunluklu interval antrenman" },
                new Hizmet { Ad = "spinning", SureDakika = 45, Aciklama = "Grup bisiklet dersi" },
                new Hizmet { Ad = "yoga", SureDakika = 60, Aciklama = "Yoga ve meditasyon" },
                new Hizmet { Ad = "pilates", SureDakika = 60, Aciklama = "Mat pilates dersi" },
                new Hizmet { Ad = "zumba", SureDakika = 60, Aciklama = "Dans karisimli kardiyo" },
                new Hizmet { Ad = "kickbox", SureDakika = 60, Aciklama = "Kickbox teknikleri" },
                new Hizmet { Ad = "fonksiyonel_antrenman", SureDakika = 45, Aciklama = "Fonksiyonel hareket antrenmani" },
                new Hizmet { Ad = "mobility_stretching", SureDakika = 45, Aciklama = "Esneklik ve hareketlilik" }
            };
            context.Hizmetler.AddRange(hizmetler);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {hizmetler.Count} Hizmetler");

            // Reload for FK references
            var allSalonlar = await context.Salonlar.ToListAsync();
            var allUzmanliklar = await context.UzmanlikAlanlari.ToListAsync();
            var allHizmetler = await context.Hizmetler.ToListAsync();

            // =============================================
            // STEP 9: Seed 50 Egitmenler with Identity accounts
            // =============================================
            var trainerNames = new (string First, string Last)[]
            {
                ("Ali", "Kaya"), ("Ayse", "Demir"), ("Mehmet", "Yilmaz"), ("Fatma", "Celik"),
                ("Mustafa", "Sahin"), ("Zeynep", "Arslan"), ("Ahmet", "Ozturk"), ("Elif", "Kilic"),
                ("Hasan", "Korkmaz"), ("Merve", "Acar"), ("Burak", "Aydin"), ("Seda", "Ozen"),
                ("Emre", "Erdogan"), ("Gamze", "Gunes"), ("Kerem", "Polat"), ("Deniz", "Aksoy"),
                ("Cem", "Yildiz"), ("Sibel", "Tekin"), ("Onur", "Dogan"), ("Ebru", "Koc"),
                ("Tolga", "Aslan"), ("Pinar", "Kurt"), ("Serkan", "Tas"), ("Hande", "Yalcin"),
                ("Murat", "Bulut"), ("Irem", "Turan"), ("Baris", "Candan"), ("Esra", "Kaplan"),
                ("Oguz", "Yuksel"), ("Yasemin", "Ozdemir"), ("Cenk", "Kara"), ("Tugba", "Sen"),
                ("Kaan", "Bayrak"), ("Serap", "Aktug"), ("Volkan", "Erdem"), ("Meltem", "Ucar"),
                ("Taner", "Ozbey"), ("Burcu", "Karaca"), ("Arda", "Yavuz"), ("Nil", "Coban"),
                ("Can", "Alkan"), ("Gul", "Basaran"), ("Selim", "Goncu"), ("Derya", "Soylu"),
                ("Alp", "Tunc"), ("Basak", "Ozkan"), ("Efe", "Dincer"), ("Ceren", "Aktas"),
                ("Berk", "Ari"), ("Melis", "Duman")
            };

            var random = new Random(42); // Fixed seed for reproducibility
            var egitmenList = new List<Egitmen>();

            for (int i = 0; i < 50; i++)
            {
                var (first, last) = trainerNames[i];
                var username = $"tr.{first.ToLower()}{last.ToLower()}";
                var email = $"{first.ToLower()}.{last.ToLower()}@fitnesscenter.com";
                var salonId = allSalonlar[(i % 20)].Id;

                // Create Identity user
                var trainerUser = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(trainerUser, DefaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(trainerUser, "Trainer");
                }

                var egitmen = new Egitmen
                {
                    AdSoyad = $"{first} {last}",
                    Email = email,
                    Telefon = $"05{30 + (i % 20):D2} {100 + i:D3} {1000 + i * 7:D4}",
                    SalonId = salonId,
                    Biyografi = "Deneyimli fitness egitmeni",
                    Aktif = true,
                    Maas = 15000 + random.Next(20000),
                    ApplicationUserId = createResult.Succeeded ? trainerUser.Id : null,
                    KullaniciAdi = username
                };

                egitmenList.Add(egitmen);
            }

            context.Egitmenler.AddRange(egitmenList);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {egitmenList.Count} Egitmenler with Identity accounts");

            // Reload egitmenler
            var allEgitmenler = await context.Egitmenler.ToListAsync();

            // =============================================
            // STEP 10: Assign Uzmanlik to Egitmenler
            // =============================================
            var egitmenUzmanlikList = new List<EgitmenUzmanlik>();
            for (int i = 0; i < allEgitmenler.Count; i++)
            {
                var uzmanlik = allUzmanliklar[i % allUzmanliklar.Count];
                egitmenUzmanlikList.Add(new EgitmenUzmanlik
                {
                    EgitmenId = allEgitmenler[i].Id,
                    UzmanlikAlaniId = uzmanlik.Id
                });
            }
            context.EgitmenUzmanliklari.AddRange(egitmenUzmanlikList);
            await context.SaveChangesAsync();
            Console.WriteLine("Assigned Uzmanlik to Egitmenler");

            // =============================================
            // STEP 11: Assign Hizmetler to Egitmenler based on Uzmanlik
            // =============================================
            var hizmetByName = allHizmetler.ToDictionary(h => h.Ad);
            var uzmanlikByName = allUzmanliklar.ToDictionary(u => u.Ad);

            var uzmanlikHizmetMap = new Dictionary<string, string[]>
            {
                { "kilo_verme", new[] { "hiit", "spinning", "fonksiyonel_antrenman" } },
                { "kas_gelistirme", new[] { "fitness", "personal_training" } },
                { "yoga", new[] { "yoga", "mobility_stretching" } },
                { "pilates", new[] { "pilates", "mobility_stretching" } },
                { "fonksiyonel", new[] { "fonksiyonel_antrenman", "hiit" } },
                { "kardiyo", new[] { "spinning", "zumba", "hiit" } },
                { "dovus_sporu", new[] { "kickbox" } },
                { "postur_mobilite", new[] { "mobility_stretching", "pilates" } }
            };

            var egitmenHizmetList = new List<EgitmenHizmet>();
            var addedPairs = new HashSet<(int, int)>();

            foreach (var eu in egitmenUzmanlikList)
            {
                var uzmanlik = allUzmanliklar.First(u => u.Id == eu.UzmanlikAlaniId);
                if (uzmanlikHizmetMap.TryGetValue(uzmanlik.Ad, out var hizmetNames))
                {
                    foreach (var hizmetAd in hizmetNames)
                    {
                        if (hizmetByName.TryGetValue(hizmetAd, out var hizmet))
                        {
                            var pair = (eu.EgitmenId, hizmet.Id);
                            if (!addedPairs.Contains(pair))
                            {
                                addedPairs.Add(pair);
                                egitmenHizmetList.Add(new EgitmenHizmet
                                {
                                    EgitmenId = eu.EgitmenId,
                                    HizmetId = hizmet.Id
                                });
                            }
                        }
                    }
                }
            }
            context.EgitmenHizmetler.AddRange(egitmenHizmetList);
            await context.SaveChangesAsync();
            Console.WriteLine("Assigned Hizmetler to Egitmenler");

            // =============================================
            // STEP 12: Create Musaitlik for all Egitmenler (7 days)
            // =============================================
            var musaitlikList = new List<Musaitlik>();
            foreach (var egitmen in allEgitmenler)
            {
                for (int day = 0; day <= 6; day++)
                {
                    musaitlikList.Add(new Musaitlik
                    {
                        EgitmenId = egitmen.Id,
                        Gun = (DayOfWeek)day,
                        BaslangicSaati = new TimeSpan(6, 0, 0),
                        BitisSaati = new TimeSpan(23, 59, 0)
                    });
                }
            }
            context.Musaitlikler.AddRange(musaitlikList);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {musaitlikList.Count} Musaitlik records");

            // =============================================
            // STEP 13: Create 200 Member Users
            // =============================================
            var memberUyeList = new List<(ApplicationUser User, Uye Uye)>();

            for (int i = 1; i <= 200; i++)
            {
                var username = $"user{i:D3}";
                var email = $"user{i:D3}@email.com";

                var memberUser = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(memberUser, DefaultPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(memberUser, "Member");

                    var uye = new Uye
                    {
                        ApplicationUserId = memberUser.Id,
                        AdSoyad = $"User{i:D3} Member",
                        Email = email
                    };
                    context.Uyeler.Add(uye);
                    memberUyeList.Add((memberUser, uye));
                }
            }
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {memberUyeList.Count} Member users and Uye records");

            // Reload Uyeler
            var allUyeler = await context.Uyeler.ToListAsync();
            var memberUyeler = allUyeler.Where(u => u.ApplicationUserId != sla55mId).ToList();

            // =============================================
            // STEP 14: Create Uyelikler (190 users, 10 without)
            // =============================================
            var uyelikList = new List<Uyelik>();
            var today = DateTime.Today;
            var twoYearsAgo = today.AddYears(-2);

            // First 10 users get no membership
            for (int i = 10; i < memberUyeler.Count; i++)
            {
                var uye = memberUyeler[i];
                
                // Determine number of memberships (1, 2, or 4)
                int membershipCount;
                if (i < 60) membershipCount = 1;
                else if (i < 120) membershipCount = 2;
                else membershipCount = 4;

                for (int m = 0; m < membershipCount; m++)
                {
                    var salonId = allSalonlar[random.Next(allSalonlar.Count)].Id;
                    var startDate = twoYearsAgo.AddDays(random.Next((int)(today - twoYearsAgo).TotalDays));
                    var endDate = startDate.AddYears(1);

                    uyelikList.Add(new Uyelik
                    {
                        UyeId = uye.Id,
                        SalonId = salonId,
                        BaslangicTarihi = startDate,
                        BitisTarihi = endDate,
                        Durum = endDate >= today ? "Aktif" : "Bitmis"
                    });
                }
            }
            context.Uyelikler.AddRange(uyelikList);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {uyelikList.Count} Uyelikler");

            // =============================================
            // STEP 15: Create Randevular (4 per member with uyelik)
            // =============================================
            var randevuList = new List<Randevu>();
            var uyelerWithMembership = memberUyeler.Skip(10).ToList();

            // Load Egitmen-Hizmet mappings
            var egitmenHizmetler = await context.EgitmenHizmetler.ToListAsync();
            var egitmenBySalon = allEgitmenler.GroupBy(e => e.SalonId).ToDictionary(g => g.Key ?? 0, g => g.ToList());

            foreach (var uye in uyelerWithMembership)
            {
                // Get active memberships for this user
                var aktifUyelikler = uyelikList.Where(u => u.UyeId == uye.Id && u.Durum == "Aktif").ToList();
                if (!aktifUyelikler.Any()) continue;

                var uyelik = aktifUyelikler.First();
                var salonId = uyelik.SalonId;

                // Get trainers for this salon
                if (!egitmenBySalon.ContainsKey(salonId) || !egitmenBySalon[salonId].Any()) continue;
                var salonEgitmenleri = egitmenBySalon[salonId];

                // Create 4 appointments
                for (int r = 0; r < 4; r++)
                {
                    var egitmen = salonEgitmenleri[random.Next(salonEgitmenleri.Count)];
                    
                    // Get services this trainer can provide
                    var trainerHizmetIds = egitmenHizmetler
                        .Where(eh => eh.EgitmenId == egitmen.Id)
                        .Select(eh => eh.HizmetId)
                        .ToList();

                    if (!trainerHizmetIds.Any()) continue;

                    var hizmet = allHizmetler.First(h => trainerHizmetIds.Contains(h.Id));

                    // Random date within membership period
                    var membershipDays = (int)(uyelik.BitisTarihi!.Value - uyelik.BaslangicTarihi).TotalDays;
                    var appointmentDate = uyelik.BaslangicTarihi.AddDays(random.Next(membershipDays));
                    
                    // Random hour between 8-20
                    var hour = 8 + random.Next(12);
                    var startTime = appointmentDate.Date.AddHours(hour);
                    var endTime = startTime.AddMinutes(hizmet.SureDakika);

                    randevuList.Add(new Randevu
                    {
                        UyeId = uye.Id,
                        SalonId = salonId,
                        EgitmenId = egitmen.Id,
                        HizmetId = hizmet.Id,
                        BaslangicZamani = startTime,
                        BitisZamani = endTime,
                        Durum = "Beklemede",
                        Notlar = null
                    });
                }
            }
            context.Randevular.AddRange(randevuList);
            await context.SaveChangesAsync();
            Console.WriteLine($"Created {randevuList.Count} Randevular (all Beklemede)");

            // =============================================
            // FINAL VERIFICATION
            // =============================================
            Console.WriteLine("\n=== VERIFICATION ===");
            Console.WriteLine($"Salonlar: {await context.Salonlar.CountAsync()}");
            Console.WriteLine($"Hizmetler: {await context.Hizmetler.CountAsync()}");
            Console.WriteLine($"UzmanlikAlanlari: {await context.UzmanlikAlanlari.CountAsync()}");
            Console.WriteLine($"Egitmenler: {await context.Egitmenler.CountAsync()}");
            Console.WriteLine($"Musaitlikler: {await context.Musaitlikler.CountAsync()}");
            Console.WriteLine($"EgitmenHizmetler: {await context.EgitmenHizmetler.CountAsync()}");
            Console.WriteLine($"EgitmenUzmanliklari: {await context.EgitmenUzmanliklari.CountAsync()}");
            Console.WriteLine($"AspNetUsers (Members): {await userManager.GetUsersInRoleAsync("Member")}");
            Console.WriteLine($"Uyeler: {await context.Uyeler.CountAsync()}");
            Console.WriteLine($"Uyelikler: {await context.Uyelikler.CountAsync()}");
            Console.WriteLine($"Randevular: {await context.Randevular.CountAsync()}");
            
            // Check protected users
            var adminCheck = await userManager.FindByNameAsync(AdminUsername);
            var sla55mCheck = await userManager.FindByNameAsync(ProtectedMemberUsername);
            Console.WriteLine($"\nAdmin preserved: {adminCheck != null}");
            Console.WriteLine($"s.la55m preserved: {sla55mCheck != null}");
            
            if (sla55mUyeId.HasValue)
            {
                var sla55mAiLogs = await context.AiLoglar.Where(a => a.UyeId == sla55mUyeId.Value).CountAsync();
                Console.WriteLine($"s.la55m AiLogs preserved: {sla55mAiLogs}");
            }

            Console.WriteLine("\n=== MASTER SEED COMPLETE ===");
        }
    }
}
