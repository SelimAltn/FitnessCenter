-- ============================================================
-- FITNESS CENTER - DATABASE RESET & SEED SCRIPT
-- CRITICAL: Run in a single transaction
-- CRITICAL: Preserves g231210558@sakarya.edu.tr (Admin)
-- CRITICAL: Preserves s.la55m user (keep AiLogs)
-- ALL TEXT IS ASCII-ONLY
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- STEP 1: Find Protected User IDs
    -- ============================================================
    DECLARE @adminId NVARCHAR(450);
    DECLARE @sla55mId NVARCHAR(450);
    DECLARE @sla55mUyeId INT;

    -- Admin user
    SELECT @adminId = Id FROM AspNetUsers WHERE UserName = 'g231210558@sakarya.edu.tr';
    PRINT 'Admin UserId: ' + ISNULL(@adminId, 'NOT FOUND');

    -- s.la55m user (could be username or email)
    SELECT @sla55mId = Id FROM AspNetUsers WHERE UserName = 's.la55m' OR Email = 's.la55m';
    PRINT 's.la55m UserId: ' + ISNULL(@sla55mId, 'NOT FOUND');

    -- Get UyeId for s.la55m
    IF @sla55mId IS NOT NULL
    BEGIN
        SELECT @sla55mUyeId = Id FROM Uyeler WHERE ApplicationUserId = @sla55mId;
        PRINT 's.la55m UyeId: ' + ISNULL(CAST(@sla55mUyeId AS VARCHAR), 'NOT FOUND');
    END

    -- ============================================================
    -- STEP 2: Delete s.la55m's Randevular and Uyelikler ONLY
    -- (Keep AiLogs intact)
    -- ============================================================
    IF @sla55mUyeId IS NOT NULL
    BEGIN
        DELETE FROM Randevular WHERE UyeId = @sla55mUyeId;
        PRINT 'Deleted s.la55m Randevular';
        
        DELETE FROM Uyelikler WHERE UyeId = @sla55mUyeId;
        PRINT 'Deleted s.la55m Uyelikler';
    END

    -- ============================================================
    -- STEP 3: General Cleanup (All Domain Data)
    -- ============================================================

    -- 3.1 Randevular (Delete all)
    DELETE FROM Randevular;
    PRINT 'Deleted all Randevular';

    -- 3.2 Mesajlar (FK to Randevu)
    DELETE FROM Mesajlar;
    PRINT 'Deleted all Mesajlar';

    -- 3.3 Musaitlikler
    DELETE FROM Musaitlikler;
    PRINT 'Deleted all Musaitlikler';

    -- 3.4 EgitmenHizmetler
    DELETE FROM EgitmenHizmetler;
    PRINT 'Deleted all EgitmenHizmetler';

    -- 3.5 EgitmenUzmanliklari
    DELETE FROM EgitmenUzmanliklari;
    PRINT 'Deleted all EgitmenUzmanliklari';

    -- 3.6 Egitmenler
    DELETE FROM Egitmenler;
    PRINT 'Deleted all Egitmenler';

    -- 3.7 Hizmetler
    DELETE FROM Hizmetler;
    PRINT 'Deleted all Hizmetler';

    -- 3.8 UzmanlikAlanlari
    DELETE FROM UzmanlikAlanlari;
    PRINT 'Deleted all UzmanlikAlanlari';

    -- 3.9 Uyelikler (all)
    DELETE FROM Uyelikler;
    PRINT 'Deleted all Uyelikler';

    -- 3.10 Salonlar
    DELETE FROM Salonlar;
    PRINT 'Deleted all Salonlar';

    -- 3.11 AiLoglar - Keep s.la55m's logs
    IF @sla55mUyeId IS NOT NULL
    BEGIN
        DELETE FROM AiLoglar WHERE UyeId IS NULL OR UyeId != @sla55mUyeId;
    END
    ELSE
    BEGIN
        DELETE FROM AiLoglar;
    END
    PRINT 'Cleaned AiLoglar (preserved s.la55m)';

    -- 3.12 Bildirimler
    DELETE FROM Bildirimler WHERE UserId NOT IN (@adminId, @sla55mId) OR UserId IS NULL;
    PRINT 'Cleaned Bildirimler (preserved admin/s.la55m)';

    -- 3.13 SupportTickets
    DELETE FROM SupportTickets WHERE UserId IS NULL OR UserId NOT IN (@adminId, @sla55mId);
    PRINT 'Cleaned SupportTickets';

    -- ============================================================
    -- STEP 4: Delete Identity Users (except admin and s.la55m)
    -- ============================================================

    -- 4.1 Clean Identity related tables first
    DELETE FROM AspNetUserTokens WHERE UserId NOT IN (@adminId, @sla55mId);
    DELETE FROM AspNetUserLogins WHERE UserId NOT IN (@adminId, @sla55mId);
    DELETE FROM AspNetUserClaims WHERE UserId NOT IN (@adminId, @sla55mId);
    DELETE FROM AspNetUserRoles WHERE UserId NOT IN (@adminId, @sla55mId);
    PRINT 'Cleaned Identity relation tables';

    -- 4.2 Uyeler tablosu - Delete all except s.la55m's Uye record
    IF @sla55mUyeId IS NOT NULL
    BEGIN
        DELETE FROM Uyeler WHERE Id != @sla55mUyeId;
    END
    ELSE
    BEGIN
        DELETE FROM Uyeler;
    END
    PRINT 'Cleaned Uyeler (preserved s.la55m)';

    -- 4.3 Delete Users
    DELETE FROM AspNetUsers WHERE Id NOT IN (@adminId, @sla55mId);
    PRINT 'Cleaned AspNetUsers (preserved admin and s.la55m)';

    -- ============================================================
    -- STEP 5: SEED - 20 Salonlar (ASCII Only)
    -- ============================================================
    PRINT 'Starting SEED...';

    -- Reset identity
    DBCC CHECKIDENT ('Salonlar', RESEED, 0);

    INSERT INTO Salonlar (Ad, Adress, Aciklama, Is24Hours, AcilisSaati, KapanisSaati) VALUES
    -- 2 Merkez (7/24)
    ('Merkez Sube 1', 'Istanbul Kadikoy Merkez', 'Ana merkez subesi, 7/24 acik', 1, NULL, NULL),
    ('Merkez Sube 2', 'Ankara Kizilay Merkez', 'Baskent merkez subesi, 7/24 acik', 1, NULL, NULL),
    -- 18 Standard (06:00-00:00)
    ('Sube Serdivan', 'Sakarya Serdivan', 'Serdivan subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Adapazari', 'Sakarya Adapazari', 'Adapazari subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Besiktas', 'Istanbul Besiktas', 'Besiktas subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Uskudar', 'Istanbul Uskudar', 'Uskudar subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Sisli', 'Istanbul Sisli', 'Sisli subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Bakirkoy', 'Istanbul Bakirkoy', 'Bakirkoy subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Pendik', 'Istanbul Pendik', 'Pendik subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Kartal', 'Istanbul Kartal', 'Kartal subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Cankaya', 'Ankara Cankaya', 'Cankaya subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Mamak', 'Ankara Mamak', 'Mamak subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Bornova', 'Izmir Bornova', 'Bornova subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Karsiyaka', 'Izmir Karsiyaka', 'Karsiyaka subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Nilüfer', 'Bursa Nilufer', 'Nilufer subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Osmangazi', 'Bursa Osmangazi', 'Osmangazi subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Seyhan', 'Adana Seyhan', 'Seyhan subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Kepez', 'Antalya Kepez', 'Kepez subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Meram', 'Konya Meram', 'Meram subesi', 0, '06:00:00', '00:00:00'),
    ('Sube Atakum', 'Samsun Atakum', 'Atakum subesi', 0, '06:00:00', '00:00:00');

    PRINT 'Created 20 Salonlar';

    -- ============================================================
    -- STEP 6: SEED - Uzmanlik Alanlari (ASCII Only)
    -- ============================================================
    DBCC CHECKIDENT ('UzmanlikAlanlari', RESEED, 0);

    INSERT INTO UzmanlikAlanlari (Ad, Aciklama, Aktif) VALUES
    ('kilo_verme', 'Kilo verme ve yag yakimi programlari', 1),
    ('kas_gelistirme', 'Kas kütlesi artirma ve vücut gelistirme', 1),
    ('yoga', 'Yoga ve nefes teknikleri', 1),
    ('pilates', 'Pilates egzersizleri', 1),
    ('fonksiyonel', 'Fonksiyonel antrenman', 1),
    ('kardiyo', 'Kardiyovasküler dayaniklilik', 1),
    ('dovus_sporu', 'Kickbox, boks ve dovus sporlari', 1),
    ('postur_mobilite', 'Postur düzeltme ve esneklik', 1);

    PRINT 'Created 8 UzmanlikAlanlari';

    -- ============================================================
    -- STEP 7: SEED - Hizmetler (ASCII Only, for each salon)
    -- ============================================================
    DBCC CHECKIDENT ('Hizmetler', RESEED, 0);

    -- Global services (not salon-specific in this schema)
    INSERT INTO Hizmetler (Ad, SureDakika, Ucret, Aciklama) VALUES
    ('fitness', 60, 300, 'Genel fitness antrenmani'),
    ('personal_training', 60, 500, 'Bire bir kisisel antrenman'),
    ('hiit', 30, 250, 'Yuksek yogunluklu interval antrenman'),
    ('spinning', 45, 200, 'Grup bisiklet dersi'),
    ('yoga', 60, 200, 'Yoga ve meditasyon'),
    ('pilates', 60, 250, 'Mat pilates dersi'),
    ('zumba', 60, 200, 'Dans karisimli kardiyo'),
    ('kickbox', 60, 300, 'Kickbox teknikleri'),
    ('fonksiyonel_antrenman', 45, 350, 'Fonksiyonel hareket antrenmani'),
    ('mobility_stretching', 45, 150, 'Esneklik ve hareketlilik');

    PRINT 'Created 10 Hizmetler';

    COMMIT TRANSACTION;
    PRINT '=== PHASE 1 COMPLETE ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
