/* =====================================================================
   SalesPulse - ÖRNEK VERİ ÜRETİM SCRIPTİ (02_seed.sql)
   ---------------------------------------------------------------------
   Ne yapar : Cities, Categories, Customers, Products, Orders, OrderDetails
              ve Payments tablolarını toplu (set-based) INSERT ile doldurur.
   Hacim    : Varsayılan 2.000.000 sipariş, 3.500.000 kalem, 100.000 müşteri,
              5.000 ürün, 30 kategori, 81 il, sipariş başına 1 ödeme.
   Gerçekçilik (case gereği):
     - Şehir yoğunluğu : büyük iller (İstanbul, Ankara, İzmir...) daha fazla müşteri
     - Sezon           : Kasım/Aralık yoğun, yaz sakin; yıllara göre hafif büyüme
     - Tekrar alışveriş: bazı müşteriler çok sipariş verir (çarpık dağılım)
     - Sipariş durumu  : Tamamlandı, Kargoda, Beklemede, İptal Edildi, İade Edildi
     - Ürün satışı     : bazı ürünler çok, bazıları az satar (çarpık dağılım)
   Tohum    : rastgelelik CHECKSUM ile deterministiktir; aynı parametrelerle
              tekrar çalıştırınca aynı veri çıkar.

   NASIL ÇALIŞTIRILIR (ÖNEMLİ):
     1) BOŞ ve YENİ bir veritabanı oluştur (örn. SalesPulseDB_Test).
     2) 01_schema.sql dosyasındaki "USE [SalesPulseDB]" satırını bu veritabanının
        adıyla değiştirip çalıştır (tablolar + indeksler + SP'ler oluşur).
     3) Aşağıdaki "USE" satırını da aynı adla değiştir.
     4) ÖNCE küçük hacimle dene: parametreleri 1000 / 500 / 20000 / 35000 yap.
     5) Sorunsuzsa varsayılan (büyük) değerlerle YENİ bir boş veritabanında çalıştır.
   Güvenlik : Tablolar boş değilse veya kimlik (IDENTITY) sayaçları sıfır değilse
              script hiçbir şey yazmadan durur; mevcut veriye dokunmaz.
   Not      : Script hata verirse veritabanını silip 01_schema.sql ile baştan kur.
   ===================================================================== */

USE [SalesPulseDB_Full];   -- <== BURAYI kendi boş test veritabanının adıyla değiştir
GO

SET NOCOUNT ON;

-- ====================== PARAMETRELER ======================
DECLARE @CustomerCount INT  = 100000;
DECLARE @ProductCount  INT  = 5000;
DECLARE @OrderCount    INT  = 2000000;
DECLARE @DetailCount   INT  = 3500000;
DECLARE @StartDate     DATE = '2023-10-01';-- ilk sipariş günü (dahil)
DECLARE @EndDate       DATE = '2026-09-01';-- son sipariş günü (HARİÇ)
DECLARE @Batch         INT  = 500000;      -- tek seferde yazılacak satır (log şişmesin diye)

-- ====================== GÜVENLİK KONTROLLERİ ======================
IF @DetailCount < @OrderCount
BEGIN
    RAISERROR(N'@DetailCount, @OrderCount''tan küçük olamaz (her siparişin en az 1 kalemi olur).', 16, 1);
    RETURN;
END

IF EXISTS (SELECT 1 FROM dbo.Orders)    OR EXISTS (SELECT 1 FROM dbo.OrderDetails)
   OR EXISTS (SELECT 1 FROM dbo.Payments)  OR EXISTS (SELECT 1 FROM dbo.Customers)
   OR EXISTS (SELECT 1 FROM dbo.Products)  OR EXISTS (SELECT 1 FROM dbo.Categories)
   OR EXISTS (SELECT 1 FROM dbo.Cities)
   OR IDENT_CURRENT('dbo.Orders') <> 1     OR IDENT_CURRENT('dbo.OrderDetails') <> 1
   OR IDENT_CURRENT('dbo.Payments') <> 1   OR IDENT_CURRENT('dbo.Customers') <> 1
   OR IDENT_CURRENT('dbo.Products') <> 1   OR IDENT_CURRENT('dbo.Categories') <> 1
BEGIN
    RAISERROR(N'Tablolar boş ve yeni olmalı. Boş bir veritabanı oluşturup önce 01_schema.sql çalıştır.', 16, 1);
    RETURN;
END

-- ====================== 1) İLLER ======================
-- Weight: müşteri dağılımında o ilin payı (büyük iller daha ağır)
CREATE TABLE #CitySeed (CityId INT PRIMARY KEY, CityName NVARCHAR(50) NOT NULL, Region NVARCHAR(50) NOT NULL, Weight INT NOT NULL);
INSERT INTO #CitySeed (CityId, CityName, Region, Weight) VALUES
    (1, N'Adana', N'Akdeniz', 3),
    (2, N'Adıyaman', N'Güneydoğu Anadolu', 1),
    (3, N'Afyonkarahisar', N'Ege', 1),
    (4, N'Ağrı', N'Doğu Anadolu', 1),
    (5, N'Amasya', N'Karadeniz', 1),
    (6, N'Ankara', N'İç Anadolu', 9),
    (7, N'Antalya', N'Akdeniz', 4),
    (8, N'Artvin', N'Karadeniz', 1),
    (9, N'Aydın', N'Ege', 2),
    (10, N'Balıkesir', N'Marmara', 2),
    (11, N'Bilecik', N'Marmara', 1),
    (12, N'Bingöl', N'Doğu Anadolu', 1),
    (13, N'Bitlis', N'Doğu Anadolu', 1),
    (14, N'Bolu', N'Karadeniz', 1),
    (15, N'Burdur', N'Akdeniz', 1),
    (16, N'Bursa', N'Marmara', 4),
    (17, N'Çanakkale', N'Marmara', 1),
    (18, N'Çankırı', N'İç Anadolu', 1),
    (19, N'Çorum', N'Karadeniz', 1),
    (20, N'Denizli', N'Ege', 2),
    (21, N'Diyarbakır', N'Güneydoğu Anadolu', 2),
    (22, N'Edirne', N'Marmara', 1),
    (23, N'Elazığ', N'Doğu Anadolu', 1),
    (24, N'Erzincan', N'Doğu Anadolu', 1),
    (25, N'Erzurum', N'Doğu Anadolu', 2),
    (26, N'Eskişehir', N'İç Anadolu', 2),
    (27, N'Gaziantep', N'Güneydoğu Anadolu', 3),
    (28, N'Giresun', N'Karadeniz', 1),
    (29, N'Gümüşhane', N'Karadeniz', 1),
    (30, N'Hakkari', N'Doğu Anadolu', 1),
    (31, N'Hatay', N'Akdeniz', 2),
    (32, N'Isparta', N'Akdeniz', 1),
    (33, N'Mersin', N'Akdeniz', 3),
    (34, N'İstanbul', N'Marmara', 20),
    (35, N'İzmir', N'Ege', 7),
    (36, N'Kars', N'Doğu Anadolu', 1),
    (37, N'Kastamonu', N'Karadeniz', 1),
    (38, N'Kayseri', N'İç Anadolu', 2),
    (39, N'Kırklareli', N'Marmara', 1),
    (40, N'Kırşehir', N'İç Anadolu', 1),
    (41, N'Kocaeli', N'Marmara', 2),
    (42, N'Konya', N'İç Anadolu', 3),
    (43, N'Kütahya', N'Ege', 1),
    (44, N'Malatya', N'Doğu Anadolu', 1),
    (45, N'Manisa', N'Ege', 2),
    (46, N'Kahramanmaraş', N'Akdeniz', 1),
    (47, N'Mardin', N'Güneydoğu Anadolu', 1),
    (48, N'Muğla', N'Ege', 2),
    (49, N'Muş', N'Doğu Anadolu', 1),
    (50, N'Nevşehir', N'İç Anadolu', 1),
    (51, N'Niğde', N'İç Anadolu', 1),
    (52, N'Ordu', N'Karadeniz', 1),
    (53, N'Rize', N'Karadeniz', 1),
    (54, N'Sakarya', N'Marmara', 2),
    (55, N'Samsun', N'Karadeniz', 2),
    (56, N'Siirt', N'Güneydoğu Anadolu', 1),
    (57, N'Sinop', N'Karadeniz', 1),
    (58, N'Sivas', N'İç Anadolu', 1),
    (59, N'Tekirdağ', N'Marmara', 2),
    (60, N'Tokat', N'Karadeniz', 1),
    (61, N'Trabzon', N'Karadeniz', 2),
    (62, N'Tunceli', N'Doğu Anadolu', 1),
    (63, N'Şanlıurfa', N'Güneydoğu Anadolu', 2),
    (64, N'Uşak', N'Ege', 1),
    (65, N'Van', N'Doğu Anadolu', 1),
    (66, N'Yozgat', N'İç Anadolu', 1),
    (67, N'Zonguldak', N'Karadeniz', 1),
    (68, N'Aksaray', N'İç Anadolu', 1),
    (69, N'Bayburt', N'Karadeniz', 1),
    (70, N'Karaman', N'İç Anadolu', 1),
    (71, N'Kırıkkale', N'İç Anadolu', 1),
    (72, N'Batman', N'Güneydoğu Anadolu', 1),
    (73, N'Şırnak', N'Güneydoğu Anadolu', 1),
    (74, N'Bartın', N'Karadeniz', 1),
    (75, N'Ardahan', N'Doğu Anadolu', 1),
    (76, N'Iğdır', N'Doğu Anadolu', 1),
    (77, N'Yalova', N'Marmara', 1),
    (78, N'Karabük', N'Karadeniz', 1),
    (79, N'Kilis', N'Güneydoğu Anadolu', 1),
    (80, N'Osmaniye', N'Akdeniz', 1),
    (81, N'Düzce', N'Karadeniz', 1);

INSERT INTO dbo.Cities (CityId, CityName, Region)
SELECT CityId, CityName, Region FROM #CitySeed;

-- Ağırlığı kadar tekrarlanan seçim tablosu: rastgele bir sıra no, ağırlıklı bir il verir
CREATE TABLE #CityPick (PickId INT IDENTITY(1,1) PRIMARY KEY, CityId INT NOT NULL);
INSERT INTO #CityPick (CityId)
SELECT cs.CityId
FROM #CitySeed cs
JOIN (SELECT TOP (50) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS k FROM sys.all_objects) w
     ON w.k <= cs.Weight
ORDER BY cs.CityId;
DECLARE @CityPickTotal INT = (SELECT COUNT(*) FROM #CityPick);

-- ====================== 2) KATEGORİLER ======================
-- BasePrice: kategorinin ortalama birim fiyatı (ürün fiyatı bunun 0.5x-2x'i)
CREATE TABLE #CatSeed (Id INT PRIMARY KEY, Name NVARCHAR(100) NOT NULL, BasePrice INT NOT NULL);
INSERT INTO #CatSeed (Id, Name, BasePrice) VALUES
    (1, N'Cep Telefonu', 18000),
    (2, N'Bilgisayar ve Tablet', 25000),
    (3, N'Televizyon ve Ses', 15000),
    (4, N'Beyaz Eşya', 14000),
    (5, N'Küçük Ev Aletleri', 1800),
    (6, N'Kadın Giyim', 600),
    (7, N'Erkek Giyim', 650),
    (8, N'Çocuk Giyim', 350),
    (9, N'Ayakkabı ve Çanta', 900),
    (10, N'Kozmetik ve Kişisel Bakım', 250),
    (11, N'Mobilya', 8000),
    (12, N'Ev Tekstili', 400),
    (13, N'Mutfak Gereçleri', 300),
    (14, N'Aydınlatma', 700),
    (15, N'Bahçe ve Yapı Market', 1500),
    (16, N'Spor ve Outdoor', 1200),
    (17, N'Fitness Ekipmanları', 2500),
    (18, N'Kitap', 90),
    (19, N'Kırtasiye', 60),
    (20, N'Oyuncak', 220),
    (21, N'Bebek Ürünleri', 350),
    (22, N'Süpermarket', 120),
    (23, N'Evcil Hayvan', 280),
    (24, N'Otomotiv', 3000),
    (25, N'Oto Aksesuar', 450),
    (26, N'Saat ve Takı', 2200),
    (27, N'Gözlük', 1600),
    (28, N'Müzik Aletleri', 4000),
    (29, N'Oyun ve Konsol', 1500),
    (30, N'Elektronik Aksesuar', 200);

INSERT INTO dbo.Categories (CategoryName)
SELECT Name FROM #CatSeed ORDER BY Id;
DECLARE @CatCount INT = (SELECT COUNT(*) FROM #CatSeed);

-- ====================== 3) YARDIMCI SAYI TABLOSU (1..N) ======================
DECLARE @MaxN INT = @OrderCount;
IF @DetailCount   > @MaxN SET @MaxN = @DetailCount;
IF @CustomerCount > @MaxN SET @MaxN = @CustomerCount;
IF @ProductCount  > @MaxN SET @MaxN = @ProductCount;

CREATE TABLE #Nums (n INT NOT NULL PRIMARY KEY);
INSERT INTO #Nums (n)
SELECT TOP (@MaxN) ROW_NUMBER() OVER (ORDER BY (SELECT NULL))
FROM sys.all_columns a CROSS JOIN sys.all_columns b;

-- ====================== 4) MÜŞTERİLER ======================
-- 20 ad x 20 soyad = 400 farklı ad-soyad; her biri yüzlerce müşteriye denk gelir
CREATE TABLE #FirstNames (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
INSERT INTO #FirstNames (Id, Name) VALUES
    (0, N'Mustafa'),
    (1, N'Ali'),
    (2, N'Elif'),
    (3, N'Mehmet'),
    (4, N'Deniz'),
    (5, N'Murat'),
    (6, N'Fatma'),
    (7, N'Emine'),
    (8, N'Ceren'),
    (9, N'Ahmet'),
    (10, N'Merve'),
    (11, N'Zeynep'),
    (12, N'Burak'),
    (13, N'Can'),
    (14, N'Ayşe'),
    (15, N'Hüseyin'),
    (16, N'Selin'),
    (17, N'Emre'),
    (18, N'Büşra'),
    (19, N'Hasan');

CREATE TABLE #LastNames (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL);
INSERT INTO #LastNames (Id, Name) VALUES
    (0, N'Kurt'),
    (1, N'Özdemir'),
    (2, N'Şimşek'),
    (3, N'Kaya'),
    (4, N'Yılmaz'),
    (5, N'Şahin'),
    (6, N'Demir'),
    (7, N'Arslan'),
    (8, N'Yıldırım'),
    (9, N'Aydın'),
    (10, N'Kara'),
    (11, N'Çelik'),
    (12, N'Doğan'),
    (13, N'Koç'),
    (14, N'Aksoy'),
    (15, N'Polat'),
    (16, N'Erdoğan'),
    (17, N'Güneş'),
    (18, N'Öztürk'),
    (19, N'Aslan');

INSERT INTO dbo.Customers (FirstName, LastName, Email, CityId, CreatedDate)
SELECT fn.Name,
       ln.Name,
       N'musteri' + CAST(x.n AS NVARCHAR(10)) + N'@example.com',
       cp.CityId,
       DATEADD(DAY, -CAST(x.h4 % 730 AS INT), @StartDate)    -- ilk siparişten en fazla 2 yıl önce kayıt
FROM (SELECT n,
             ABS(CAST(CHECKSUM(n, 1) AS BIGINT)) % 20 AS fi,                    -- ad seçimi
             ABS(CAST(CHECKSUM(n, 2) AS BIGINT)) % 20 AS li,                    -- soyad seçimi
             ABS(CAST(CHECKSUM(n, 3) AS BIGINT)) % @CityPickTotal + 1 AS ci,    -- ağırlıklı il seçimi
             ABS(CAST(CHECKSUM(n, 4) AS BIGINT)) AS h4
      FROM #Nums WHERE n <= @CustomerCount) x
JOIN #FirstNames fn ON fn.Id = x.fi
JOIN #LastNames  ln ON ln.Id = x.li
JOIN #CityPick   cp ON cp.PickId = x.ci
ORDER BY x.n;

-- ====================== 5) ÜRÜNLER ======================
-- Ürün adı: "<Kategori> Model <No>"; fiyat = kategori taban fiyatı x (0.5 - 2.0)
INSERT INTO dbo.Products (ProductName, CategoryId, UnitPrice)
SELECT cs.Name + N' Model ' + CAST(x.n AS NVARCHAR(10)),
       cs.Id,
       CAST(ROUND(cs.BasePrice * (0.5 + 1.5 * CAST(x.r AS FLOAT) / 1000000.0), 0) AS DECIMAL(10,2))
FROM (SELECT n,
             ((n - 1) % @CatCount) + 1 AS cid,
             ABS(CAST(CHECKSUM(n, 5) AS BIGINT)) % 1000000 AS r
      FROM #Nums WHERE n <= @ProductCount) x
JOIN #CatSeed cs ON cs.Id = x.cid
ORDER BY x.n;

-- ====================== 6) SEZONLU GÜN TABLOSU ======================
-- Ay ağırlığı: Kasım/Aralık yoğun. Yıl çarpanı: sonraki yıllar biraz daha yoğun.
CREATE TABLE #MonthW (M INT PRIMARY KEY, W INT NOT NULL);
INSERT INTO #MonthW (M, W) VALUES (1,8),(2,7),(3,8),(4,8),(5,8),(6,7),(7,7),(8,7),(9,8),(10,9),(11,12),(12,11);

CREATE TABLE #DayPick (PickId INT IDENTITY(1,1) PRIMARY KEY, OrderDay DATE NOT NULL);
INSERT INTO #DayPick (OrderDay)
SELECT d.D
FROM (SELECT TOP (DATEDIFF(DAY, @StartDate, @EndDate))
             DATEADD(DAY, ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) - 1, @StartDate) AS D
      FROM sys.all_columns a CROSS JOIN sys.all_columns b) d
JOIN #MonthW mw ON mw.M = MONTH(d.D)
JOIN (SELECT TOP (300) ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS k FROM sys.all_objects) w
     ON w.k <= mw.W * (4 + DATEDIFF(YEAR, @StartDate, d.D))
ORDER BY d.D;
DECLARE @DayPickTotal INT = (SELECT COUNT(*) FROM #DayPick);

-- ====================== 7) SİPARİŞLER (parça parça) ======================
-- CustomerId: POWER(u, 1.3) ile düşük numaralı müşteriler daha çok sipariş verir
-- Durum dağılımı: %65 Tamamlandı, %12 Kargoda, %8 Beklemede, %9 İptal, %6 İade
-- TotalAmount şimdilik 0; kalemler eklenince 9. adımda hesaplanacak
DECLARE @From INT = 1;
DECLARE @To   INT;

WHILE @From <= @OrderCount
BEGIN
    SET @To = @From + @Batch - 1;
    IF @To > @OrderCount SET @To = @OrderCount;

    INSERT INTO dbo.Orders (CustomerId, OrderDate, TotalAmount, Status)
    SELECT 1 + CAST(FLOOR(@CustomerCount * POWER(CAST(x.u AS FLOAT) / 1000000.0, 1.3)) AS INT),
           DATEADD(SECOND, CAST(x.sec AS INT), CAST(dp.OrderDay AS DATETIME2(0))),
           0,
           CASE WHEN x.s < 65 THEN N'Tamamlandı'
                WHEN x.s < 77 THEN N'Kargoda'
                WHEN x.s < 85 THEN N'Beklemede'
                WHEN x.s < 94 THEN N'İptal Edildi'
                ELSE N'İade Edildi' END
    FROM (SELECT n,
                 ABS(CAST(CHECKSUM(n, 11) AS BIGINT)) % 1000000 AS u,
                 ABS(CAST(CHECKSUM(n, 12) AS BIGINT)) % @DayPickTotal + 1 AS dpick,
                 ABS(CAST(CHECKSUM(n, 13) AS BIGINT)) % 86400 AS sec,
                 ABS(CAST(CHECKSUM(n, 14) AS BIGINT)) % 100 AS s
          FROM #Nums WHERE n BETWEEN @From AND @To) x
    JOIN #DayPick dp ON dp.PickId = x.dpick
    ORDER BY x.n;

    SET @From = @To + 1;
END

-- ====================== 8) SİPARİŞ KALEMLERİ (parça parça) ======================
-- İlk @OrderCount kalem: her siparişe 1 kalem (hiç kalemsiz sipariş kalmaz)
-- Kalan kalemler: rastgele siparişlere eklenir
-- ProductId: POWER(u, 2) ile düşük numaralı ürünler çok, yüksek numaralılar az satar
-- Fiyat: ürünün güncel birim fiyatı
SET @From = 1;
WHILE @From <= @DetailCount
BEGIN
    SET @To = @From + @Batch - 1;
    IF @To > @DetailCount SET @To = @DetailCount;

    INSERT INTO dbo.OrderDetails (OrderId, ProductId, Quantity, UnitPrice)
    SELECT y.OrderId, y.ProductId, y.Qty, p.UnitPrice
    FROM (SELECT n,
                 ABS(CAST(CHECKSUM(n, 21) AS BIGINT)) AS h1,
                 ABS(CAST(CHECKSUM(n, 22) AS BIGINT)) AS h2,
                 ABS(CAST(CHECKSUM(n, 23) AS BIGINT)) AS h3
          FROM #Nums WHERE n BETWEEN @From AND @To) x
    CROSS APPLY (SELECT
            CASE WHEN x.n <= @OrderCount THEN x.n
                 ELSE CAST(1 + x.h1 % @OrderCount AS INT) END AS OrderId,
            CAST(1 + FLOOR(@ProductCount * POWER(CAST(x.h2 % 1000000 AS FLOAT) / 1000000.0, 2.0)) AS INT) AS ProductId,
            CASE WHEN x.h3 % 100 < 60 THEN 1
                 WHEN x.h3 % 100 < 85 THEN 2
                 WHEN x.h3 % 100 < 93 THEN 3
                 WHEN x.h3 % 100 < 98 THEN 4
                 ELSE 5 END AS Qty) y
    JOIN dbo.Products p ON p.ProductId = y.ProductId
    ORDER BY x.n;

    SET @From = @To + 1;
END

-- ====================== 9) SİPARİŞ TUTARLARI ======================
-- TotalAmount = o siparişin kalemlerinin (adet x birim fiyat) toplamı
SET @From = 1;
WHILE @From <= @OrderCount
BEGIN
    SET @To = @From + @Batch - 1;
    IF @To > @OrderCount SET @To = @OrderCount;

    UPDATE o
    SET o.TotalAmount = d.Total
    FROM dbo.Orders o
    JOIN (SELECT OrderId, SUM(Quantity * UnitPrice) AS Total
          FROM dbo.OrderDetails
          WHERE OrderId BETWEEN @From AND @To
          GROUP BY OrderId) d ON d.OrderId = o.OrderId
    WHERE o.OrderId BETWEEN @From AND @To;

    SET @From = @To + 1;
END

-- ====================== 10) ÖDEMELER (sipariş başına tam 1) ======================
-- Yöntem dağılımı: %62 Kredi Kartı, %15 Banka Kartı, %13 Havale/EFT, %10 Kapıda Ödeme
SET @From = 1;
WHILE @From <= @OrderCount
BEGIN
    SET @To = @From + @Batch - 1;
    IF @To > @OrderCount SET @To = @OrderCount;

    INSERT INTO dbo.Payments (OrderId, PaymentMethod, Amount)
    SELECT o.OrderId,
           CASE WHEN z.h < 62 THEN N'Kredi Kartı'
                WHEN z.h < 77 THEN N'Banka Kartı'
                WHEN z.h < 90 THEN N'Havale/EFT'
                ELSE N'Kapıda Ödeme' END,
           o.TotalAmount
    FROM dbo.Orders o
    CROSS APPLY (SELECT ABS(CAST(CHECKSUM(o.OrderId, 31) AS BIGINT)) % 100 AS h) z
    WHERE o.OrderId BETWEEN @From AND @To
    ORDER BY o.OrderId;

    SET @From = @To + 1;
END

-- ====================== 11) İSTATİSTİKLERİ GÜNCELLE ======================
EXEC sp_updatestats;

-- ====================== 12) KONTROL ÇIKTISI ======================
-- Satır sayıları
SELECT t.name AS Tablo, SUM(ps.row_count) AS SatirSayisi
FROM sys.tables t
JOIN sys.dm_db_partition_stats ps ON ps.object_id = t.object_id AND ps.index_id IN (0, 1)
GROUP BY t.name
ORDER BY SatirSayisi DESC;

-- Tutarlılık: ikisi de 0 olmalı
SELECT
    (SELECT COUNT(*) FROM dbo.Orders o WHERE NOT EXISTS (SELECT 1 FROM dbo.OrderDetails d WHERE d.OrderId = o.OrderId)) AS KalemsizSiparis,
    (SELECT COUNT(*) FROM dbo.Orders o JOIN dbo.Payments p ON p.OrderId = o.OrderId WHERE p.Amount <> o.TotalAmount) AS TutarUyusmayanOdeme;
GO
