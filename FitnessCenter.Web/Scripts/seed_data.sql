-- ============================================
-- FitnessCenter Database Seed Script
-- Tarih: 2025-12-18
-- Açıklama: 3 Salon, 40 Eğitmen, 100 Kullanıcı, Üyelikler ve Randevular
-- ============================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Seed işlemi başlıyor...';

    -- ============================================
    -- 1. SALONLAR (3 adet - farklı şehirler)
    -- ============================================
    PRINT '1. Salonlar ekleniyor...';

    -- Mevcut salonları temizle (opsiyonel - yorum satırı yapılabilir)
    -- DELETE FROM Salonlar;

    -- Yeni salonları ekle (mevcut değilse)
    IF NOT EXISTS (SELECT 1 FROM Salonlar WHERE Ad = 'FitZone İstanbul Kadıköy')
    BEGIN
        INSERT INTO Salonlar (Ad, Adress, Aciklama)
        VALUES ('FitZone İstanbul Kadıköy', 'İstanbul / Kadıköy - Bağdat Caddesi No:123', 'Premium fitness merkezi. Kardiyo, ağırlık ve grup ders stüdyoları mevcut.');
    END

    IF NOT EXISTS (SELECT 1 FROM Salonlar WHERE Ad = 'FitZone Ankara Çankaya')
    BEGIN
        INSERT INTO Salonlar (Ad, Adress, Aciklama)
        VALUES ('FitZone Ankara Çankaya', 'Ankara / Çankaya - Tunalı Hilmi Caddesi No:45', 'Modern spor salonu. Yüzme havuzu ve spa hizmetleri de sunulmaktadır.');
    END

    IF NOT EXISTS (SELECT 1 FROM Salonlar WHERE Ad = 'FitZone Sakarya Serdivan')
    BEGIN
        INSERT INTO Salonlar (Ad, Adress, Aciklama)
        VALUES ('FitZone Sakarya Serdivan', 'Sakarya / Serdivan - Üniversite Caddesi No:78', 'Aile dostu fitness merkezi. Çocuk oyun alanı ve kafeterya mevcuttur.');
    END

    -- Salon ID'lerini al
    DECLARE @SalonIstanbul INT = (SELECT Id FROM Salonlar WHERE Ad = 'FitZone İstanbul Kadıköy');
    DECLARE @SalonAnkara INT = (SELECT Id FROM Salonlar WHERE Ad = 'FitZone Ankara Çankaya');
    DECLARE @SalonSakarya INT = (SELECT Id FROM Salonlar WHERE Ad = 'FitZone Sakarya Serdivan');

    PRINT 'Salonlar eklendi. Istanbul ID: ' + CAST(@SalonIstanbul AS VARCHAR) + ', Ankara ID: ' + CAST(@SalonAnkara AS VARCHAR) + ', Sakarya ID: ' + CAST(@SalonSakarya AS VARCHAR);

    -- ============================================
    -- 2. TRAINER ROLE ID
    -- ============================================
    DECLARE @TrainerRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Trainer');
    DECLARE @MemberRoleId NVARCHAR(450) = (SELECT Id FROM AspNetRoles WHERE Name = 'Member');

    -- Eğer roller yoksa oluştur
    IF @TrainerRoleId IS NULL
    BEGIN
        SET @TrainerRoleId = NEWID();
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@TrainerRoleId, 'Trainer', 'TRAINER', NEWID());
    END

    IF @MemberRoleId IS NULL
    BEGIN
        SET @MemberRoleId = NEWID();
        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@MemberRoleId, 'Member', 'MEMBER', NEWID());
    END

    -- ============================================
    -- 3. UZMANLIK ALANLARI ID'leri
    -- ============================================
    DECLARE @UzmanlikFitness INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Fitness');
    DECLARE @UzmanlikYoga INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Yoga');
    DECLARE @UzmanlikPilates INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Pilates');
    DECLARE @UzmanlikCrossFit INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'CrossFit');
    DECLARE @UzmanlikRehabilitasyon INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Rehabilitasyon');
    DECLARE @UzmanlikBeslenme INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Beslenme');
    DECLARE @UzmanlikKisiselAntrenman INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Kişisel Antrenman');
    DECLARE @UzmanlikGrupDersleri INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Grup Dersleri');
    DECLARE @UzmanlikHIIT INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'HIIT');
    DECLARE @UzmanlikMasaj INT = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Ad = 'Masaj');

    -- ============================================
    -- 4. HİZMET ID'leri
    -- ============================================
    DECLARE @HizmetKisiselAntrenman INT = (SELECT TOP 1 Id FROM Hizmetler WHERE Ad = 'Kişisel Antrenman');
    DECLARE @HizmetGrupFitness INT = (SELECT TOP 1 Id FROM Hizmetler WHERE Ad = 'Grup Fitness Dersi');
    DECLARE @HizmetSporcuMasaji INT = (SELECT TOP 1 Id FROM Hizmetler WHERE Ad = 'Sporcu Masajı');

    -- Varsayılan hizmet (randevular için)
    IF @HizmetKisiselAntrenman IS NULL
        SET @HizmetKisiselAntrenman = (SELECT TOP 1 Id FROM Hizmetler);

    -- ============================================
    -- 5. EĞİTMENLER (40 adet)
    -- ============================================
    PRINT '2. Eğitmenler ekleniyor...';

    -- Identity uyumlu şifre hash'i (User123! için)
    -- ASP.NET Core Identity V3 formatında hash
    DECLARE @PasswordHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAELfxQHxKvh8e7M9WS7a7y5+O8T8R0KxT3J5GvS5dZ+F5q5R5K+L5M5N5O5P5Q5R5S5T5U5=';
    
    -- Eğitmen verileri için temp tablo
    DECLARE @EgitmenlerTemp TABLE (
        RowNum INT IDENTITY(1,1),
        AdSoyad NVARCHAR(100),
        Email NVARCHAR(200),
        Telefon NVARCHAR(20),
        KullaniciAdi NVARCHAR(50),
        Maas DECIMAL(18,2),
        Biyografi NVARCHAR(1000),
        SalonId INT
    );

    -- İstanbul Eğitmenleri (14 kişi)
    INSERT INTO @EgitmenlerTemp (AdSoyad, Email, Telefon, KullaniciAdi, Maas, Biyografi, SalonId) VALUES
    ('Ali Yıldırım', 'ali.yildirim@fitzone.com', '0532 101 0001', 'ali.yildirim', 22000.00, 'CrossFit ve fonksiyonel antrenman uzmanı. 8 yıllık deneyim.', @SalonIstanbul),
    ('Zeynep Kara', 'zeynep.kara@fitzone.com', '0532 101 0002', 'zeynep.kara', 25000.00, 'Yoga ve pilates eğitmeni. Uluslararası sertifikalı.', @SalonIstanbul),
    ('Murat Çelik', 'murat.celik@fitzone.com', '0532 101 0003', 'murat.celik', 28000.00, 'Vücut geliştirme ve beslenme danışmanı. IFBB Pro kartı sahibi.', @SalonIstanbul),
    ('Elif Demir', 'elif.demir@fitzone.com', '0532 101 0004', 'elif.demir', 21000.00, 'Grup fitness ve aerobik eğitmeni. Enerjik dersler.', @SalonIstanbul),
    ('Burak Öztürk', 'burak.ozturk@fitzone.com', '0532 101 0005', 'burak.ozturk', 24000.00, 'HIIT ve kardiyo uzmanı. Kilo verme programları.', @SalonIstanbul),
    ('Selin Aydın', 'selin.aydin@fitzone.com', '0532 101 0006', 'selin.aydin', 23000.00, 'Zumba ve dans fitness eğitmeni.', @SalonIstanbul),
    ('Can Yılmaz', 'can.yilmaz@fitzone.com', '0532 101 0007', 'can.yilmaz', 26000.00, 'Kickboks ve dövüş sanatları antrenörü.', @SalonIstanbul),
    ('Ayşe Şahin', 'ayse.sahin@fitzone.com', '0532 101 0008', 'ayse.sahin', 27000.00, 'Rehabilitasyon ve fizyoterapi uzmanı.', @SalonIstanbul),
    ('Emre Koç', 'emre.koc@fitzone.com', '0532 101 0009', 'emre.koc', 20000.00, 'TRX ve fonksiyonel antrenman eğitmeni.', @SalonIstanbul),
    ('Deniz Arslan', 'deniz.arslan@fitzone.com', '0532 101 0010', 'deniz.arslan', 22500.00, 'Yüzme ve su sporları antrenörü.', @SalonIstanbul),
    ('Gökhan Polat', 'gokhan.polat@fitzone.com', '0532 101 0011', 'gokhan.polat', 29000.00, 'Powerlifting ve güç antrenmanı uzmanı.', @SalonIstanbul),
    ('Merve Yıldız', 'merve.yildiz@fitzone.com', '0532 101 0012', 'merve.yildiz', 21500.00, 'Barre ve pilates reformer eğitmeni.', @SalonIstanbul),
    ('Kerem Aksoy', 'kerem.aksoy@fitzone.com', '0532 101 0013', 'kerem.aksoy', 23500.00, 'Kalistenik ve street workout antrenörü.', @SalonIstanbul),
    ('Pınar Güneş', 'pinar.gunes@fitzone.com', '0532 101 0014', 'pinar.gunes', 24500.00, 'Prenatal ve postnatal fitness uzmanı.', @SalonIstanbul);

    -- Ankara Eğitmenleri (13 kişi)
    INSERT INTO @EgitmenlerTemp (AdSoyad, Email, Telefon, KullaniciAdi, Maas, Biyografi, SalonId) VALUES
    ('Ahmet Korkmaz', 'ahmet.korkmaz@fitzone.com', '0532 102 0001', 'ahmet.korkmaz', 25000.00, 'Kişisel antrenman ve performans koçu.', @SalonAnkara),
    ('Fatma Erdoğan', 'fatma.erdogan@fitzone.com', '0532 102 0002', 'fatma.erdogan', 22000.00, 'Mat pilates ve core antrenman uzmanı.', @SalonAnkara),
    ('Hasan Kaya', 'hasan.kaya@fitzone.com', '0532 102 0003', 'hasan.kaya', 27000.00, 'Fitness ve vücut geliştirme antrenörü.', @SalonAnkara),
    ('İrem Yalçın', 'irem.yalcin@fitzone.com', '0532 102 0004', 'irem.yalcin', 23000.00, 'Yoga terapi ve meditasyon eğitmeni.', @SalonAnkara),
    ('Oğuz Temiz', 'oguz.temiz@fitzone.com', '0532 102 0005', 'oguz.temiz', 24000.00, 'CrossFit Level 2 sertifikalı antrenör.', @SalonAnkara),
    ('Ceren Özkan', 'ceren.ozkan@fitzone.com', '0532 102 0006', 'ceren.ozkan', 21000.00, 'Spinning ve indoor cycling eğitmeni.', @SalonAnkara),
    ('Serkan Başar', 'serkan.basar@fitzone.com', '0532 102 0007', 'serkan.basar', 28000.00, 'Olimpik kaldırış ve güç antrenörü.', @SalonAnkara),
    ('Nilgün Acar', 'nilgun.acar@fitzone.com', '0532 102 0008', 'nilgun.acar', 26000.00, 'Sporcu masajı ve recovery uzmanı.', @SalonAnkara),
    ('Tolga Demirtaş', 'tolga.demirtas@fitzone.com', '0532 102 0009', 'tolga.demirtas', 22500.00, 'Fonksiyonel fitness ve mobilite eğitmeni.', @SalonAnkara),
    ('Esra Kılıç', 'esra.kilic@fitzone.com', '0532 102 0010', 'esra.kilic', 20000.00, 'Step ve aerobik eğitmeni.', @SalonAnkara),
    ('Barış Tunç', 'baris.tunc@fitzone.com', '0532 102 0011', 'baris.tunc', 25500.00, 'MMA ve savunma sanatları antrenörü.', @SalonAnkara),
    ('Melis Çetin', 'melis.cetin@fitzone.com', '0532 102 0012', 'melis.cetin', 23500.00, 'Esneklik ve stretching uzmanı.', @SalonAnkara),
    ('Uğur Yavuz', 'ugur.yavuz@fitzone.com', '0532 102 0013', 'ugur.yavuz', 24500.00, 'Atletik performans ve sprint antrenörü.', @SalonAnkara);

    -- Sakarya Eğitmenleri (13 kişi)
    INSERT INTO @EgitmenlerTemp (AdSoyad, Email, Telefon, KullaniciAdi, Maas, Biyografi, SalonId) VALUES
    ('Mustafa Özdemir', 'mustafa.ozdemir@fitzone.com', '0532 103 0001', 'mustafa.ozdemir', 24000.00, 'Fitness ve kondisyon antrenörü. 12 yıl tecrübe.', @SalonSakarya),
    ('Büşra Karagöz', 'busra.karagoz@fitzone.com', '0532 103 0002', 'busra.karagoz', 21000.00, 'Pilates ve yoga eğitmeni.', @SalonSakarya),
    ('Onur Şen', 'onur.sen@fitzone.com', '0532 103 0003', 'onur.sen', 27000.00, 'Personal training ve beslenme koçu.', @SalonSakarya),
    ('Gamze Özer', 'gamze.ozer@fitzone.com', '0532 103 0004', 'gamze.ozer', 22000.00, 'Grup fitness ve zumba eğitmeni.', @SalonSakarya),
    ('Cem Akın', 'cem.akin@fitzone.com', '0532 103 0005', 'cem.akin', 25000.00, 'HIIT ve tabata uzmanı.', @SalonSakarya),
    ('Seda Doğan', 'seda.dogan@fitzone.com', '0532 103 0006', 'seda.dogan', 23000.00, 'Boks ve fitness boxing eğitmeni.', @SalonSakarya),
    ('Volkan Kurt', 'volkan.kurt@fitzone.com', '0532 103 0007', 'volkan.kurt', 28000.00, 'Profesyonel vücut geliştirme antrenörü.', @SalonSakarya),
    ('Aslı Ergün', 'asli.ergun@fitzone.com', '0532 103 0008', 'asli.ergun', 26000.00, 'Kadın fitness ve form tutma uzmanı.', @SalonSakarya),
    ('Tarık Bulut', 'tarik.bulut@fitzone.com', '0532 103 0009', 'tarik.bulut', 22500.00, 'CrossFit ve outdoor antrenman eğitmeni.', @SalonSakarya),
    ('Dilek Yıldırım', 'dilek.yildirim@fitzone.com', '0532 103 0010', 'dilek.yildirim', 20000.00, 'Dans ve ritim dersleri eğitmeni.', @SalonSakarya),
    ('Erhan Aslan', 'erhan.aslan@fitzone.com', '0532 103 0011', 'erhan.aslan', 25500.00, 'Plyo ve atlama antrenmanları uzmanı.', @SalonSakarya),
    ('Nur Tekin', 'nur.tekin@fitzone.com', '0532 103 0012', 'nur.tekin', 21500.00, 'Hamile ve yeni anne fitness programları.', @SalonSakarya),
    ('Kaan Reis', 'kaan.reis@fitzone.com', '0532 103 0013', 'kaan.reis', 23500.00, 'Yüzme ve su jimnastiği antrenörü.', @SalonSakarya);

    -- Her eğitmen için Identity user ve Egitmen kaydı oluştur
    DECLARE @i INT = 1;
    DECLARE @MaxEgitmen INT = (SELECT COUNT(*) FROM @EgitmenlerTemp);
    DECLARE @UserId NVARCHAR(450);
    DECLARE @AdSoyad NVARCHAR(100);
    DECLARE @EgitmenEmail NVARCHAR(200);
    DECLARE @Telefon NVARCHAR(20);
    DECLARE @KullaniciAdi NVARCHAR(50);
    DECLARE @Maas DECIMAL(18,2);
    DECLARE @Biyografi NVARCHAR(1000);
    DECLARE @SalonId INT;
    DECLARE @EgitmenId INT;

    WHILE @i <= @MaxEgitmen
    BEGIN
        SELECT 
            @AdSoyad = AdSoyad,
            @EgitmenEmail = Email,
            @Telefon = Telefon,
            @KullaniciAdi = KullaniciAdi,
            @Maas = Maas,
            @Biyografi = Biyografi,
            @SalonId = SalonId
        FROM @EgitmenlerTemp 
        WHERE RowNum = @i;

        -- Eğitmen zaten var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM Egitmenler WHERE Email = @EgitmenEmail)
        BEGIN
            -- Identity User oluştur
            SET @UserId = NEWID();
            
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                                     EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                                     PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
                                     LockoutEnabled, AccessFailedCount)
            VALUES (@UserId, @KullaniciAdi, UPPER(@KullaniciAdi), @EgitmenEmail, UPPER(@EgitmenEmail),
                    1, @PasswordHash, NEWID(), NEWID(),
                    @Telefon, 0, 0,
                    1, 0);

            -- Trainer rolü ata
            INSERT INTO AspNetUserRoles (UserId, RoleId)
            VALUES (@UserId, @TrainerRoleId);

            -- Eğitmen kaydı oluştur
            INSERT INTO Egitmenler (AdSoyad, Email, Telefon, KullaniciAdi, SifreHash, SalonId, Maas, Biyografi, Aktif, ApplicationUserId)
            VALUES (@AdSoyad, @EgitmenEmail, @Telefon, @KullaniciAdi, @PasswordHash, @SalonId, @Maas, @Biyografi, 1, @UserId);
        END

        SET @i = @i + 1;
    END

    PRINT 'Eğitmenler eklendi.';

    -- ============================================
    -- 6. EĞİTMEN UZMANLIKLARI (her eğitmene 1-3 adet)
    -- ============================================
    PRINT '3. Eğitmen uzmanlıkları ekleniyor...';

    -- Tüm eğitmenleri döngüyle işle
    DECLARE @EgitmenCursor CURSOR;
    DECLARE @RandomUzmanlik1 INT, @RandomUzmanlik2 INT, @RandomUzmanlik3 INT;
    DECLARE @UzmanlikSayisi INT;

    SET @EgitmenCursor = CURSOR FOR 
        SELECT Id FROM Egitmenler WHERE NOT EXISTS (SELECT 1 FROM EgitmenUzmanliklari WHERE EgitmenId = Egitmenler.Id);

    OPEN @EgitmenCursor;
    FETCH NEXT FROM @EgitmenCursor INTO @EgitmenId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Her eğitmene 1-3 uzmanlık (rastgele)
        SET @UzmanlikSayisi = (ABS(CHECKSUM(NEWID())) % 3) + 1; -- 1, 2 veya 3

        -- Uzmanlık alanlarını seç
        SET @RandomUzmanlik1 = (SELECT TOP 1 Id FROM UzmanlikAlanlari ORDER BY NEWID());
        SET @RandomUzmanlik2 = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Id != @RandomUzmanlik1 ORDER BY NEWID());
        SET @RandomUzmanlik3 = (SELECT TOP 1 Id FROM UzmanlikAlanlari WHERE Id NOT IN (@RandomUzmanlik1, @RandomUzmanlik2) ORDER BY NEWID());

        -- İlk uzmanlık (her zaman)
        IF NOT EXISTS (SELECT 1 FROM EgitmenUzmanliklari WHERE EgitmenId = @EgitmenId AND UzmanlikAlaniId = @RandomUzmanlik1)
            INSERT INTO EgitmenUzmanliklari (EgitmenId, UzmanlikAlaniId) VALUES (@EgitmenId, @RandomUzmanlik1);

        -- İkinci uzmanlık (2 veya 3 ise)
        IF @UzmanlikSayisi >= 2 AND NOT EXISTS (SELECT 1 FROM EgitmenUzmanliklari WHERE EgitmenId = @EgitmenId AND UzmanlikAlaniId = @RandomUzmanlik2)
            INSERT INTO EgitmenUzmanliklari (EgitmenId, UzmanlikAlaniId) VALUES (@EgitmenId, @RandomUzmanlik2);

        -- Üçüncü uzmanlık (3 ise)
        IF @UzmanlikSayisi >= 3 AND NOT EXISTS (SELECT 1 FROM EgitmenUzmanliklari WHERE EgitmenId = @EgitmenId AND UzmanlikAlaniId = @RandomUzmanlik3)
            INSERT INTO EgitmenUzmanliklari (EgitmenId, UzmanlikAlaniId) VALUES (@EgitmenId, @RandomUzmanlik3);

        FETCH NEXT FROM @EgitmenCursor INTO @EgitmenId;
    END

    CLOSE @EgitmenCursor;
    DEALLOCATE @EgitmenCursor;

    PRINT 'Eğitmen uzmanlıkları eklendi.';

    -- ============================================
    -- 7. EĞİTMEN ÇALIŞMA SAATLERİ (Musaitlik)
    -- ============================================
    PRINT '4. Eğitmen çalışma saatleri ekleniyor...';

    -- Her eğitmen için haftada 4-6 gün çalışma saati
    DECLARE @GunSayisi INT;
    DECLARE @Gun INT;
    DECLARE @BaslangicSaat INT;
    DECLARE @GunlerArray TABLE (Gun INT);

    SET @EgitmenCursor = CURSOR FOR 
        SELECT Id FROM Egitmenler WHERE NOT EXISTS (SELECT 1 FROM Musaitlikler WHERE EgitmenId = Egitmenler.Id);

    OPEN @EgitmenCursor;
    FETCH NEXT FROM @EgitmenCursor INTO @EgitmenId;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- Haftada 4-6 gün
        SET @GunSayisi = (ABS(CHECKSUM(NEWID())) % 3) + 4; -- 4, 5 veya 6

        -- Günleri temizle ve yeniden doldur
        DELETE FROM @GunlerArray;
        
        -- Rastgele günler seç (0=Pazar, 1=Pazartesi, ... 6=Cumartesi)
        INSERT INTO @GunlerArray (Gun)
        SELECT TOP (@GunSayisi) number 
        FROM (VALUES (1),(2),(3),(4),(5),(6),(0)) AS Days(number) 
        ORDER BY NEWID();

        -- Başlangıç saati (8, 9, 10, veya 11)
        SET @BaslangicSaat = (ABS(CHECKSUM(NEWID())) % 4) + 8;

        -- Her gün için çalışma saati ekle
        INSERT INTO Musaitlikler (EgitmenId, Gun, BaslangicSaati, BitisSaati)
        SELECT @EgitmenId, Gun, 
               CAST(DATEADD(HOUR, @BaslangicSaat, '00:00:00') AS TIME),
               CAST(DATEADD(HOUR, @BaslangicSaat + 8, '00:00:00') AS TIME) -- 8 saat çalışma
        FROM @GunlerArray;

        FETCH NEXT FROM @EgitmenCursor INTO @EgitmenId;
    END

    CLOSE @EgitmenCursor;
    DEALLOCATE @EgitmenCursor;

    PRINT 'Eğitmen çalışma saatleri eklendi.';

    -- ============================================
    -- 8. KULLANICILAR (100 adet)
    -- ============================================
    PRINT '5. Kullanıcılar (üyeler) ekleniyor...';

    -- Kullanıcı verileri için temp tablo
    DECLARE @KullanicilarTemp TABLE (
        RowNum INT IDENTITY(1,1),
        AdSoyad NVARCHAR(100),
        Email NVARCHAR(200),
        Telefon NVARCHAR(20),
        KullaniciAdi NVARCHAR(50)
    );

    -- 100 kullanıcı ekle
    INSERT INTO @KullanicilarTemp (AdSoyad, Email, Telefon, KullaniciAdi) VALUES
    ('Mehmet Yılmaz', 'mehmet.yilmaz@email.com', '0533 200 0001', 'mehmet.yilmaz'),
    ('Fatma Demir', 'fatma.demir@email.com', '0533 200 0002', 'fatma.demir'),
    ('Ali Kaya', 'ali.kaya@email.com', '0533 200 0003', 'ali.kaya'),
    ('Zeynep Çelik', 'zeynep.celik@email.com', '0533 200 0004', 'zeynep.celik'),
    ('Hüseyin Şahin', 'huseyin.sahin@email.com', '0533 200 0005', 'huseyin.sahin'),
    ('Emine Yıldız', 'emine.yildiz@email.com', '0533 200 0006', 'emine.yildiz'),
    ('Mustafa Öztürk', 'mustafa.ozturk@email.com', '0533 200 0007', 'mustafa.ozturk'),
    ('Hatice Aydın', 'hatice.aydin@email.com', '0533 200 0008', 'hatice.aydin'),
    ('İbrahim Arslan', 'ibrahim.arslan@email.com', '0533 200 0009', 'ibrahim.arslan'),
    ('Ayşe Doğan', 'ayse.dogan@email.com', '0533 200 0010', 'ayse.dogan'),
    ('Ahmet Polat', 'ahmet.polat@email.com', '0533 200 0011', 'ahmet.polat'),
    ('Meryem Koç', 'meryem.koc@email.com', '0533 200 0012', 'meryem.koc'),
    ('Hasan Yavuz', 'hasan.yavuz@email.com', '0533 200 0013', 'hasan.yavuz'),
    ('Sultan Korkmaz', 'sultan.korkmaz@email.com', '0533 200 0014', 'sultan.korkmaz'),
    ('Ömer Aksoy', 'omer.aksoy@email.com', '0533 200 0015', 'omer.aksoy'),
    ('Havva Erdoğan', 'havva.erdogan@email.com', '0533 200 0016', 'havva.erdogan'),
    ('Osman Tekin', 'osman.tekin@email.com', '0533 200 0017', 'osman.tekin'),
    ('Hanife Kurt', 'hanife.kurt@email.com', '0533 200 0018', 'hanife.kurt'),
    ('Yusuf Özkan', 'yusuf.ozkan@email.com', '0533 200 0019', 'yusuf.ozkan'),
    ('Rabia Güneş', 'rabia.gunes@email.com', '0533 200 0020', 'rabia.gunes'),
    ('Kadir Tunç', 'kadir.tunc@email.com', '0533 200 0021', 'kadir.tunc'),
    ('Fadime Aslan', 'fadime.aslan@email.com', '0533 200 0022', 'fadime.aslan'),
    ('Kemal Şen', 'kemal.sen@email.com', '0533 200 0023', 'kemal.sen'),
    ('Zübeyde Eren', 'zubeyde.eren@email.com', '0533 200 0024', 'zubeyde.eren'),
    ('Recep Demirtaş', 'recep.demirtas@email.com', '0533 200 0025', 'recep.demirtas'),
    ('Hacer Bulut', 'hacer.bulut@email.com', '0533 200 0026', 'hacer.bulut'),
    ('Süleyman Ateş', 'suleyman.ates@email.com', '0533 200 0027', 'suleyman.ates'),
    ('Cemile Yalçın', 'cemile.yalcin@email.com', '0533 200 0028', 'cemile.yalcin'),
    ('Murat Kılıç', 'murat.kilic@email.com', '0533 200 0029', 'murat.kilic'),
    ('Selma Özdemir', 'selma.ozdemir@email.com', '0533 200 0030', 'selma.ozdemir'),
    ('Tuncay Güler', 'tuncay.guler@email.com', '0533 200 0031', 'tuncay.guler'),
    ('Nuray Karaca', 'nuray.karaca@email.com', '0533 200 0032', 'nuray.karaca'),
    ('Serdar Acar', 'serdar.acar@email.com', '0533 200 0033', 'serdar.acar'),
    ('Sevgi Başar', 'sevgi.basar@email.com', '0533 200 0034', 'sevgi.basar'),
    ('Erkan Çetin', 'erkan.cetin@email.com', '0533 200 0035', 'erkan.cetin'),
    ('Gülsüm Özer', 'gulsum.ozer@email.com', '0533 200 0036', 'gulsum.ozer'),
    ('Emrah Karagöz', 'emrah.karagoz@email.com', '0533 200 0037', 'emrah.karagoz'),
    ('Dilek Yılmaz', 'dilek.yilmaz.user@email.com', '0533 200 0038', 'dilek.yilmaz.user'),
    ('Burak Demir', 'burak.demir.user@email.com', '0533 200 0039', 'burak.demir.user'),
    ('Derya Kaya', 'derya.kaya@email.com', '0533 200 0040', 'derya.kaya'),
    ('Gökhan Çelik', 'gokhan.celik@email.com', '0533 200 0041', 'gokhan.celik'),
    ('Sibel Şahin', 'sibel.sahin@email.com', '0533 200 0042', 'sibel.sahin'),
    ('Deniz Yıldız', 'deniz.yildiz.user@email.com', '0533 200 0043', 'deniz.yildiz.user'),
    ('Tolga Öztürk', 'tolga.ozturk@email.com', '0533 200 0044', 'tolga.ozturk'),
    ('Hakan Aydın', 'hakan.aydin@email.com', '0533 200 0045', 'hakan.aydin'),
    ('Pelin Arslan', 'pelin.arslan@email.com', '0533 200 0046', 'pelin.arslan'),
    ('Çağla Doğan', 'cagla.dogan@email.com', '0533 200 0047', 'cagla.dogan'),
    ('Umut Polat', 'umut.polat@email.com', '0533 200 0048', 'umut.polat'),
    ('İlker Koç', 'ilker.koc@email.com', '0533 200 0049', 'ilker.koc'),
    ('Meltem Yavuz', 'meltem.yavuz@email.com', '0533 200 0050', 'meltem.yavuz'),
    ('Barış Korkmaz', 'baris.korkmaz@email.com', '0533 200 0051', 'baris.korkmaz'),
    ('Canan Aksoy', 'canan.aksoy@email.com', '0533 200 0052', 'canan.aksoy'),
    ('Alper Erdoğan', 'alper.erdogan@email.com', '0533 200 0053', 'alper.erdogan'),
    ('Gamze Tekin', 'gamze.tekin@email.com', '0533 200 0054', 'gamze.tekin'),
    ('Fikret Kurt', 'fikret.kurt@email.com', '0533 200 0055', 'fikret.kurt'),
    ('Sema Özkan', 'sema.ozkan@email.com', '0533 200 0056', 'sema.ozkan'),
    ('Volkan Güneş', 'volkan.gunes@email.com', '0533 200 0057', 'volkan.gunes'),
    ('Özge Tunç', 'ozge.tunc@email.com', '0533 200 0058', 'ozge.tunc'),
    ('Mete Aslan', 'mete.aslan@email.com', '0533 200 0059', 'mete.aslan'),
    ('Burcu Şen', 'burcu.sen@email.com', '0533 200 0060', 'burcu.sen'),
    ('Cenk Eren', 'cenk.eren@email.com', '0533 200 0061', 'cenk.eren'),
    ('Ece Demirtaş', 'ece.demirtas@email.com', '0533 200 0062', 'ece.demirtas'),
    ('Koray Bulut', 'koray.bulut@email.com', '0533 200 0063', 'koray.bulut'),
    ('Tuğba Ateş', 'tugba.ates@email.com', '0533 200 0064', 'tugba.ates'),
    ('Arda Yalçın', 'arda.yalcin@email.com', '0533 200 0065', 'arda.yalcin'),
    ('Melek Kılıç', 'melek.kilic@email.com', '0533 200 0066', 'melek.kilic'),
    ('Orhan Özdemir', 'orhan.ozdemir@email.com', '0533 200 0067', 'orhan.ozdemir'),
    ('Filiz Güler', 'filiz.guler@email.com', '0533 200 0068', 'filiz.guler'),
    ('Tayfun Karaca', 'tayfun.karaca@email.com', '0533 200 0069', 'tayfun.karaca'),
    ('İnci Acar', 'inci.acar@email.com', '0533 200 0070', 'inci.acar'),
    ('Oktay Başar', 'oktay.basar@email.com', '0533 200 0071', 'oktay.basar'),
    ('Nilüfer Çetin', 'nilufer.cetin@email.com', '0533 200 0072', 'nilufer.cetin'),
    ('Engin Özer', 'engin.ozer@email.com', '0533 200 0073', 'engin.ozer'),
    ('Yasemin Karagöz', 'yasemin.karagoz@email.com', '0533 200 0074', 'yasemin.karagoz'),
    ('Rıza Yılmaz', 'riza.yilmaz@email.com', '0533 200 0075', 'riza.yilmaz'),
    ('Zehra Demir', 'zehra.demir@email.com', '0533 200 0076', 'zehra.demir'),
    ('Ferhat Kaya', 'ferhat.kaya@email.com', '0533 200 0077', 'ferhat.kaya'),
    ('Gizem Çelik', 'gizem.celik@email.com', '0533 200 0078', 'gizem.celik'),
    ('Levent Şahin', 'levent.sahin@email.com', '0533 200 0079', 'levent.sahin'),
    ('Berna Yıldız', 'berna.yildiz@email.com', '0533 200 0080', 'berna.yildiz'),
    ('Fırat Öztürk', 'firat.ozturk@email.com', '0533 200 0081', 'firat.ozturk'),
    ('Esma Aydın', 'esma.aydin@email.com', '0533 200 0082', 'esma.aydin'),
    ('Yasin Arslan', 'yasin.arslan@email.com', '0533 200 0083', 'yasin.arslan'),
    ('Özlem Doğan', 'ozlem.dogan@email.com', '0533 200 0084', 'ozlem.dogan'),
    ('Birol Polat', 'birol.polat@email.com', '0533 200 0085', 'birol.polat'),
    ('Neslihan Koç', 'neslihan.koc@email.com', '0533 200 0086', 'neslihan.koc'),
    ('Cem Yavuz', 'cem.yavuz@email.com', '0533 200 0087', 'cem.yavuz'),
    ('Saadet Korkmaz', 'saadet.korkmaz@email.com', '0533 200 0088', 'saadet.korkmaz'),
    ('Halil Aksoy', 'halil.aksoy@email.com', '0533 200 0089', 'halil.aksoy'),
    ('Nazan Erdoğan', 'nazan.erdogan@email.com', '0533 200 0090', 'nazan.erdogan'),
    ('Tamer Tekin', 'tamer.tekin@email.com', '0533 200 0091', 'tamer.tekin'),
    ('Şermin Kurt', 'sermin.kurt@email.com', '0533 200 0092', 'sermin.kurt'),
    ('Doğukan Özkan', 'dogukan.ozkan@email.com', '0533 200 0093', 'dogukan.ozkan'),
    ('Bahar Güneş', 'bahar.gunes@email.com', '0533 200 0094', 'bahar.gunes'),
    ('İlhan Tunç', 'ilhan.tunc@email.com', '0533 200 0095', 'ilhan.tunc'),
    ('Seher Aslan', 'seher.aslan@email.com', '0533 200 0096', 'seher.aslan'),
    ('Onur Şen', 'onur.sen.user@email.com', '0533 200 0097', 'onur.sen.user'),
    ('Ayla Eren', 'ayla.eren@email.com', '0533 200 0098', 'ayla.eren'),
    ('Cengiz Demirtaş', 'cengiz.demirtas@email.com', '0533 200 0099', 'cengiz.demirtas'),
    ('Selin Bulut', 'selin.bulut@email.com', '0533 200 0100', 'selin.bulut');

    -- Her kullanıcı için Identity user ve Uye kaydı oluştur
    DECLARE @j INT = 1;
    DECLARE @MaxKullanici INT = (SELECT COUNT(*) FROM @KullanicilarTemp);
    DECLARE @KullaniciEmail NVARCHAR(200);
    DECLARE @KullaniciTelefon NVARCHAR(20);
    DECLARE @KullaniciAdiTemp NVARCHAR(50);
    DECLARE @AdSoyadTemp NVARCHAR(100);
    DECLARE @UyeId INT;

    WHILE @j <= @MaxKullanici
    BEGIN
        SELECT 
            @AdSoyadTemp = AdSoyad,
            @KullaniciEmail = Email,
            @KullaniciTelefon = Telefon,
            @KullaniciAdiTemp = KullaniciAdi
        FROM @KullanicilarTemp 
        WHERE RowNum = @j;

        -- Kullanıcı zaten var mı kontrol et
        IF NOT EXISTS (SELECT 1 FROM Uyeler WHERE Email = @KullaniciEmail)
        BEGIN
            -- Identity User oluştur
            SET @UserId = NEWID();
            
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, 
                                     EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
                                     PhoneNumber, PhoneNumberConfirmed, TwoFactorEnabled, 
                                     LockoutEnabled, AccessFailedCount)
            VALUES (@UserId, @KullaniciAdiTemp, UPPER(@KullaniciAdiTemp), @KullaniciEmail, UPPER(@KullaniciEmail),
                    1, @PasswordHash, NEWID(), NEWID(),
                    @KullaniciTelefon, 0, 0,
                    1, 0);

            -- Member rolü ata
            INSERT INTO AspNetUserRoles (UserId, RoleId)
            VALUES (@UserId, @MemberRoleId);

            -- Uye kaydı oluştur
            INSERT INTO Uyeler (AdSoyad, Email, Telefon, ApplicationUserId)
            VALUES (@AdSoyadTemp, @KullaniciEmail, @KullaniciTelefon, @UserId);
        END

        SET @j = @j + 1;
    END

    PRINT 'Kullanıcılar eklendi.';

    -- ============================================
    -- 9. ÜYELİKLER (%70 kullanıcı, 1-2 salon)
    -- ============================================
    PRINT '6. Üyelikler ekleniyor...';

    -- Üye olacak kullanıcıları seç (ilk 70 kişi)
    DECLARE @UyeIdCursor CURSOR;
    DECLARE @RandomSalon1 INT, @RandomSalon2 INT;
    DECLARE @UyelikSayisi INT;
    DECLARE @Counter INT = 0;

    SET @UyeIdCursor = CURSOR FOR 
        SELECT Id FROM Uyeler WHERE NOT EXISTS (SELECT 1 FROM Uyelikler WHERE UyeId = Uyeler.Id)
        ORDER BY Id;

    OPEN @UyeIdCursor;
    FETCH NEXT FROM @UyeIdCursor INTO @UyeId;

    WHILE @@FETCH_STATUS = 0 AND @Counter < 70
    BEGIN
        -- 1 veya 2 salon üyeliği (rastgele)
        SET @UyelikSayisi = CASE WHEN ABS(CHECKSUM(NEWID())) % 100 < 30 THEN 2 ELSE 1 END; -- %30 ihtimalle 2 salon

        -- Rastgele salonlar seç
        SET @RandomSalon1 = (SELECT TOP 1 Id FROM Salonlar ORDER BY NEWID());
        SET @RandomSalon2 = (SELECT TOP 1 Id FROM Salonlar WHERE Id != @RandomSalon1 ORDER BY NEWID());

        -- İlk üyelik
        IF NOT EXISTS (SELECT 1 FROM Uyelikler WHERE UyeId = @UyeId AND SalonId = @RandomSalon1)
        BEGIN
            INSERT INTO Uyelikler (UyeId, SalonId, BaslangicTarihi, BitisTarihi, Durum)
            VALUES (@UyeId, @RandomSalon1, DATEADD(MONTH, -3, GETDATE()), DATEADD(MONTH, 9, GETDATE()), 'Aktif');
        END

        -- İkinci üyelik (eğer 2 salon seçildiyse)
        IF @UyelikSayisi = 2 AND NOT EXISTS (SELECT 1 FROM Uyelikler WHERE UyeId = @UyeId AND SalonId = @RandomSalon2)
        BEGIN
            INSERT INTO Uyelikler (UyeId, SalonId, BaslangicTarihi, BitisTarihi, Durum)
            VALUES (@UyeId, @RandomSalon2, DATEADD(MONTH, -1, GETDATE()), DATEADD(MONTH, 11, GETDATE()), 'Aktif');
        END

        SET @Counter = @Counter + 1;
        FETCH NEXT FROM @UyeIdCursor INTO @UyeId;
    END

    CLOSE @UyeIdCursor;
    DEALLOCATE @UyeIdCursor;

    PRINT 'Üyelikler eklendi.';

    -- ============================================
    -- 10. RANDEVULAR (üyeliği olanlar için)
    -- ============================================
    PRINT '7. Randevular oluşturuluyor...';

    -- Randevu oluşturma
    DECLARE @RandevuCount INT = 0;
    DECLARE @TargetRandevuCount INT = 200; -- Hedef randevu sayısı
    DECLARE @RandevuTarih DATE;
    DECLARE @RandevuSaat TIME;
    DECLARE @RandevuBaslangic DATETIME;
    DECLARE @RandevuBitis DATETIME;
    DECLARE @RandevuEgitmenId INT;
    DECLARE @RandevuSalonId INT;
    DECLARE @RandomDayOffset INT;
    DECLARE @RandomHour INT;

    -- Üyeliği olan her üye için randevu oluştur
    DECLARE @UyelikCursor CURSOR;
    DECLARE @UyelikUyeId INT, @UyelikSalonId INT;
    DECLARE @DayOfWeek INT;
    DECLARE @MusaitBaslangic TIME, @MusaitBitis TIME;
    DECLARE @MusaitBaslangicHour INT, @MusaitBitisHour INT;
    DECLARE @RandomHizmetId INT;

    SET @UyelikCursor = CURSOR FOR 
        SELECT DISTINCT UyeId, SalonId FROM Uyelikler WHERE Durum = 'Aktif';

    OPEN @UyelikCursor;
    FETCH NEXT FROM @UyelikCursor INTO @UyelikUyeId, @UyelikSalonId;

    WHILE @@FETCH_STATUS = 0 AND @RandevuCount < @TargetRandevuCount
    BEGIN
        -- Bu salonda çalışan bir eğitmen seç
        SELECT TOP 1 @RandevuEgitmenId = Id 
        FROM Egitmenler 
        WHERE SalonId = @UyelikSalonId AND Aktif = 1
        ORDER BY NEWID();

        IF @RandevuEgitmenId IS NOT NULL
        BEGIN
            -- Rastgele tarih: -30 ile +30 gün arası
            SET @RandomDayOffset = (ABS(CHECKSUM(NEWID())) % 61) - 30;
            SET @RandevuTarih = DATEADD(DAY, @RandomDayOffset, CAST(GETDATE() AS DATE));

            -- Eğitmenin o gün çalışıp çalışmadığını kontrol et
            SET @DayOfWeek = DATEPART(WEEKDAY, @RandevuTarih) - 1; -- 0=Pazar, 1=Pazartesi, ...
            -- SQL Server'da WEEKDAY: 1=Pazar, 2=Pazartesi, ... -> DayOfWeek enum: 0=Pazar, 1=Pazartesi, ...
            
            SET @MusaitBaslangic = NULL;
            SET @MusaitBitis = NULL;
            
            SELECT TOP 1 
                @MusaitBaslangic = BaslangicSaati, 
                @MusaitBitis = BitisSaati
            FROM Musaitlikler 
            WHERE EgitmenId = @RandevuEgitmenId AND Gun = @DayOfWeek;

            IF @MusaitBaslangic IS NOT NULL
            BEGIN
                -- Çalışma saatleri içinde rastgele bir saat seç
                SET @MusaitBaslangicHour = DATEPART(HOUR, @MusaitBaslangic);
                SET @MusaitBitisHour = DATEPART(HOUR, @MusaitBitis) - 1; -- 1 saat önce bitir (randevu süresi için)

                IF @MusaitBitisHour > @MusaitBaslangicHour
                BEGIN
                    SET @RandomHour = @MusaitBaslangicHour + (ABS(CHECKSUM(NEWID())) % (@MusaitBitisHour - @MusaitBaslangicHour + 1));
                    
                    SET @RandevuBaslangic = CAST(@RandevuTarih AS DATETIME) + CAST(DATEADD(HOUR, @RandomHour, '00:00:00') AS DATETIME);
                    SET @RandevuBitis = DATEADD(HOUR, 1, @RandevuBaslangic);

                    -- Çakışma kontrolü
                    IF NOT EXISTS (
                        SELECT 1 FROM Randevular 
                        WHERE EgitmenId = @RandevuEgitmenId 
                        AND @RandevuBaslangic < BitisZamani 
                        AND @RandevuBitis > BaslangicZamani
                    )
                    BEGIN
                        -- Hizmet seç
                        SELECT TOP 1 @RandomHizmetId = Id FROM Hizmetler ORDER BY NEWID();

                        INSERT INTO Randevular (SalonId, HizmetId, EgitmenId, UyeId, BaslangicZamani, BitisZamani, Notlar, Durum)
                        VALUES (@UyelikSalonId, @RandomHizmetId, @RandevuEgitmenId, @UyelikUyeId, 
                                @RandevuBaslangic, @RandevuBitis, 
                                'Seed verisi ile oluşturulmuş randevu.', 'Beklemede');

                        SET @RandevuCount = @RandevuCount + 1;
                    END
                END
            END
        END

        FETCH NEXT FROM @UyelikCursor INTO @UyelikUyeId, @UyelikSalonId;
        
        -- Döngüyü yeniden başlat gerekirse
        IF @@FETCH_STATUS <> 0 AND @RandevuCount < @TargetRandevuCount
        BEGIN
            CLOSE @UyelikCursor;
            OPEN @UyelikCursor;
            FETCH NEXT FROM @UyelikCursor INTO @UyelikUyeId, @UyelikSalonId;
        END
    END

    CLOSE @UyelikCursor;
    DEALLOCATE @UyelikCursor;

    PRINT 'Randevular eklendi. Toplam: ' + CAST(@RandevuCount AS VARCHAR);

    -- ============================================
    -- TRANSACTION COMMIT
    -- ============================================
    COMMIT TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT 'SEED İŞLEMİ BAŞARIYLA TAMAMLANDI!';
    PRINT '========================================';
    PRINT '';

    -- Özet bilgi için değişkenler
    DECLARE @SalonCount INT, @EgitmenCount INT, @UzmanlikCount INT;
    DECLARE @MusaitlikCount INT, @UyeCount INT, @UyelikCount INT, @RandevuTotalCount INT;
    
    SELECT @SalonCount = COUNT(*) FROM Salonlar;
    SELECT @EgitmenCount = COUNT(*) FROM Egitmenler;
    SELECT @UzmanlikCount = COUNT(*) FROM EgitmenUzmanliklari;
    SELECT @MusaitlikCount = COUNT(*) FROM Musaitlikler;
    SELECT @UyeCount = COUNT(*) FROM Uyeler;
    SELECT @UyelikCount = COUNT(*) FROM Uyelikler;
    SELECT @RandevuTotalCount = COUNT(*) FROM Randevular;

    PRINT 'ÖZET:';
    PRINT '- Salonlar: ' + CAST(@SalonCount AS VARCHAR);
    PRINT '- Eğitmenler: ' + CAST(@EgitmenCount AS VARCHAR);
    PRINT '- Eğitmen Uzmanlıkları: ' + CAST(@UzmanlikCount AS VARCHAR);
    PRINT '- Çalışma Saatleri: ' + CAST(@MusaitlikCount AS VARCHAR);
    PRINT '- Üyeler: ' + CAST(@UyeCount AS VARCHAR);
    PRINT '- Üyelikler: ' + CAST(@UyelikCount AS VARCHAR);
    PRINT '- Randevular: ' + CAST(@RandevuTotalCount AS VARCHAR);

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT 'HATA OLUŞTU! İşlem geri alındı.';
    PRINT '========================================';
    PRINT 'Hata Mesajı: ' + ERROR_MESSAGE();
    PRINT 'Hata Satırı: ' + CAST(ERROR_LINE() AS VARCHAR);
END CATCH

SET NOCOUNT OFF;
