-- ============================================================
-- FITNESS CENTER - SEED PHASE 2
-- Trainers (50), Trainer Accounts, Musaitlik, Assignments
-- ============================================================

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================================
    -- STEP 1: Create Trainer Identity Accounts
    -- ============================================================
    
    -- Note: This script creates trainers without Identity accounts
    -- Identity accounts need to be created via C# UserManager
    -- We'll create the Egitmen records with placeholder ApplicationUserId
    
    DBCC CHECKIDENT ('Egitmenler', RESEED, 0);
    
    -- 50 Trainers distributed across 20 salons (2-3 per salon)
    -- ASCII names only, Maas between 15000-35000
    
    DECLARE @salonCount INT = 20;
    DECLARE @i INT = 1;
    
    -- Trainer names (ASCII only)
    DECLARE @names TABLE (id INT IDENTITY(1,1), firstname VARCHAR(50), lastname VARCHAR(50));
    INSERT INTO @names (firstname, lastname) VALUES
    ('Ali', 'Kaya'), ('Ayse', 'Demir'), ('Mehmet', 'Yilmaz'), ('Fatma', 'Celik'),
    ('Mustafa', 'Sahin'), ('Zeynep', 'Arslan'), ('Ahmet', 'Ozturk'), ('Elif', 'Kilic'),
    ('Hasan', 'Korkmaz'), ('Merve', 'Acar'), ('Burak', 'Aydin'), ('Seda', 'Ozen'),
    ('Emre', 'Erdogan'), ('Gamze', 'Gunes'), ('Kerem', 'Polat'), ('Deniz', 'Aksoy'),
    ('Cem', 'Yildiz'), ('Sibel', 'Tekin'), ('Onur', 'Dogan'), ('Ebru', 'Koc'),
    ('Tolga', 'Aslan'), ('Pinar', 'Kurt'), ('Serkan', 'Tas'), ('Hande', 'Yalcin'),
    ('Murat', 'Bulut'), ('Irem', 'Turan'), ('Baris', 'Candan'), ('Esra', 'Kaplan'),
    ('Oguz', 'Yuksel'), ('Yasemin', 'Ozdemir'), ('Cenk', 'Kara'), ('Tugba', 'Sen'),
    ('Kaan', 'Bayrak'), ('Serap', 'Aktug'), ('Volkan', 'Erdem'), ('Meltem', 'Ucar'),
    ('Taner', 'Ozbey'), ('Burcu', 'Karaca'), ('Arda', 'Yavuz'), ('Nil', 'Coban'),
    ('Can', 'Alkan'), ('Gul', 'Basaran'), ('Selim', 'Goncu'), ('Derya', 'Soylu'),
    ('Alp', 'Tunc'), ('Basak', 'Ozkan'), ('Efe', 'Dincer'), ('Ceren', 'Aktas'),
    ('Berk', 'Ari'), ('Melis', 'Duman');
    
    -- Create 50 trainers
    DECLARE @salonId INT;
    DECLARE @fname VARCHAR(50);
    DECLARE @lname VARCHAR(50);
    DECLARE @fullname VARCHAR(100);
    DECLARE @email VARCHAR(200);
    DECLARE @maas DECIMAL(18,2);
    DECLARE @trainerId INT;
    
    WHILE @i <= 50
    BEGIN
        -- Distribute across salons (2-3 per salon)
        SET @salonId = ((@i - 1) % 20) + 1;
        
        SELECT @fname = firstname, @lname = lastname FROM @names WHERE id = @i;
        SET @fullname = @fname + ' ' + @lname;
        SET @email = LOWER(@fname) + '.' + LOWER(@lname) + '@fitnesscenter.com';
        SET @maas = 15000 + (RAND(CHECKSUM(NEWID())) * 20000); -- 15000-35000
        
        INSERT INTO Egitmenler (AdSoyad, Email, Telefon, SalonId, Biyografi, Aktif, Maas, ApplicationUserId, KullaniciAdi, SifreHash)
        VALUES (
            @fullname,
            @email,
            '05' + RIGHT('00' + CAST(30 + (@i % 20) AS VARCHAR), 2) + ' ' + 
                RIGHT('000' + CAST(100 + @i AS VARCHAR), 3) + ' ' +
                RIGHT('0000' + CAST(1000 + @i * 7 AS VARCHAR), 4),
            @salonId,
            'Deneyimli fitness egitmeni',
            1,
            @maas,
            NULL, -- Will be set when Identity account is created
            'tr.' + LOWER(@fname) + LOWER(@lname),
            NULL -- Will be set when Identity account is created
        );
        
        SET @i = @i + 1;
    END
    
    PRINT 'Created 50 Egitmenler';

    -- ============================================================
    -- STEP 2: Assign Uzmanlik to Trainers
    -- ============================================================
    
    -- Get uzmanlik IDs
    DECLARE @uzKiloVerme INT, @uzKasGelistirme INT, @uzYoga INT, @uzPilates INT;
    DECLARE @uzFonksiyonel INT, @uzKardiyo INT, @uzDovus INT, @uzPostur INT;
    
    SELECT @uzKiloVerme = Id FROM UzmanlikAlanlari WHERE Ad = 'kilo_verme';
    SELECT @uzKasGelistirme = Id FROM UzmanlikAlanlari WHERE Ad = 'kas_gelistirme';
    SELECT @uzYoga = Id FROM UzmanlikAlanlari WHERE Ad = 'yoga';
    SELECT @uzPilates = Id FROM UzmanlikAlanlari WHERE Ad = 'pilates';
    SELECT @uzFonksiyonel = Id FROM UzmanlikAlanlari WHERE Ad = 'fonksiyonel';
    SELECT @uzKardiyo = Id FROM UzmanlikAlanlari WHERE Ad = 'kardiyo';
    SELECT @uzDovus = Id FROM UzmanlikAlanlari WHERE Ad = 'dovus_sporu';
    SELECT @uzPostur = Id FROM UzmanlikAlanlari WHERE Ad = 'postur_mobilite';
    
    -- Assign expertise based on trainer ID distribution
    INSERT INTO EgitmenUzmanliklari (EgitmenId, UzmanlikAlaniId)
    SELECT e.Id, 
        CASE (e.Id % 8)
            WHEN 0 THEN @uzKiloVerme
            WHEN 1 THEN @uzKasGelistirme
            WHEN 2 THEN @uzYoga
            WHEN 3 THEN @uzPilates
            WHEN 4 THEN @uzFonksiyonel
            WHEN 5 THEN @uzKardiyo
            WHEN 6 THEN @uzDovus
            WHEN 7 THEN @uzPostur
        END
    FROM Egitmenler e;
    
    PRINT 'Assigned Uzmanlik to all Egitmenler';

    -- ============================================================
    -- STEP 3: Assign Hizmetler to Trainers based on Uzmanlik
    -- ============================================================
    
    -- Get hizmet IDs
    DECLARE @hFitness INT, @hPersonal INT, @hHiit INT, @hSpinning INT, @hYoga INT;
    DECLARE @hPilates INT, @hZumba INT, @hKickbox INT, @hFonksiyonel INT, @hMobility INT;
    
    SELECT @hFitness = Id FROM Hizmetler WHERE Ad = 'fitness';
    SELECT @hPersonal = Id FROM Hizmetler WHERE Ad = 'personal_training';
    SELECT @hHiit = Id FROM Hizmetler WHERE Ad = 'hiit';
    SELECT @hSpinning = Id FROM Hizmetler WHERE Ad = 'spinning';
    SELECT @hYoga = Id FROM Hizmetler WHERE Ad = 'yoga';
    SELECT @hPilates = Id FROM Hizmetler WHERE Ad = 'pilates';
    SELECT @hZumba = Id FROM Hizmetler WHERE Ad = 'zumba';
    SELECT @hKickbox = Id FROM Hizmetler WHERE Ad = 'kickbox';
    SELECT @hFonksiyonel = Id FROM Hizmetler WHERE Ad = 'fonksiyonel_antrenman';
    SELECT @hMobility = Id FROM Hizmetler WHERE Ad = 'mobility_stretching';
    
    -- kilo_verme -> hiit, spinning, fonksiyonel_antrenman
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hHiit FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKiloVerme;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hSpinning FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKiloVerme;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hFonksiyonel FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKiloVerme;
    
    -- kas_gelistirme -> fitness, personal_training
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hFitness FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKasGelistirme;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hPersonal FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKasGelistirme;
    
    -- yoga -> yoga, mobility_stretching
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hYoga FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzYoga;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hMobility FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzYoga;
    
    -- pilates -> pilates, mobility_stretching
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hPilates FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzPilates;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hMobility FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzPilates
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hMobility);
    
    -- fonksiyonel -> fonksiyonel_antrenman, hiit
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hFonksiyonel FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzFonksiyonel
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hFonksiyonel);
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hHiit FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzFonksiyonel
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hHiit);
    
    -- kardiyo -> spinning, zumba, hiit
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hSpinning FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKardiyo
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hSpinning);
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hZumba FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKardiyo;
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hHiit FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzKardiyo
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hHiit);
    
    -- dovus_sporu -> kickbox
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hKickbox FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzDovus;
    
    -- postur_mobilite -> mobility_stretching, pilates
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hMobility FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzPostur
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hMobility);
    INSERT INTO EgitmenHizmetler (EgitmenId, HizmetId)
    SELECT eu.EgitmenId, @hPilates FROM EgitmenUzmanliklari eu WHERE eu.UzmanlikAlaniId = @uzPostur
    AND NOT EXISTS (SELECT 1 FROM EgitmenHizmetler eh WHERE eh.EgitmenId = eu.EgitmenId AND eh.HizmetId = @hPilates);
    
    PRINT 'Assigned Hizmetler to Egitmenler based on Uzmanlik';

    -- ============================================================
    -- STEP 4: Create Musaitlik for all trainers (7 days)
    -- ============================================================
    
    -- Salon working hours: 
    -- Is24Hours=1: 00:00-23:59
    -- Is24Hours=0: 06:00-00:00
    
    -- For all trainers, create 7 days of availability
    DECLARE @dayOfWeek INT = 0; -- 0=Sunday, 1=Monday...6=Saturday
    
    WHILE @dayOfWeek <= 6
    BEGIN
        -- 24/7 salons (SalonId 1, 2)
        INSERT INTO Musaitlikler (EgitmenId, Gun, BaslangicSaati, BitisSaati)
        SELECT e.Id, @dayOfWeek, '06:00:00', '23:59:00'
        FROM Egitmenler e
        WHERE e.SalonId IN (1, 2);
        
        -- Standard salons (06:00-00:00)
        INSERT INTO Musaitlikler (EgitmenId, Gun, BaslangicSaati, BitisSaati)
        SELECT e.Id, @dayOfWeek, '06:00:00', '23:59:00'
        FROM Egitmenler e
        WHERE e.SalonId NOT IN (1, 2);
        
        SET @dayOfWeek = @dayOfWeek + 1;
    END
    
    PRINT 'Created Musaitlik for all Egitmenler (7 days each)';

    COMMIT TRANSACTION;
    PRINT '=== PHASE 2 COMPLETE ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
