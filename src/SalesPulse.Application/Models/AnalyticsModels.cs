namespace SalesPulse.Application.Models;

// Sales Intelligence (Gelişmiş Satış Analizi) veri modelleri. Özellik adları saklı yordamların
// kolon adlarıyla birebir aynıdır; Dapper eşlemeyi ada göre yapar.

/// <summary>Ciroya göre ilk 20 müşteri (usp_GetTopCustomers).</summary>
public sealed class TopCustomerDto
{
    public int CustomerId { get; set; }
    public string MusteriAdi { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
    public decimal ToplamCiro { get; set; }
}

/// <summary>Kategorilerin aylık performansı, ay ve kategori başına bir satır (usp_GetCategoryMonthlyPerformance).</summary>
public sealed class CategoryMonthlyDto
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SatisAdedi { get; set; }
    public decimal ToplamCiro { get; set; }
}

/// <summary>Tekrar alışveriş yapan müşteri oranı (usp_GetRepeatCustomerRate).</summary>
public sealed class RepeatCustomerRateDto
{
    public int AktifMusteri { get; set; }
    public int TekrarEdenMusteri { get; set; }
    public decimal TekrarOrani { get; set; }
}

/// <summary>Tarih aralığı karşılaştırması: Donem 1 = seçili aralık, Donem 2 = karşılaştırılan aralık (usp_GetDateRangeComparison).</summary>
public sealed class PeriodComparisonDto
{
    public int Donem { get; set; }
    public int SiparisSayisi { get; set; }
    public decimal ToplamCiro { get; set; }
    public int AktifMusteri { get; set; }
    public decimal OrtalamaSepet { get; set; }
}

/// <summary>İki dönemli karşılaştırma sorgusu. Bitiş tarihleri SQL'de dışarıda tutulur (< EndDate).</summary>
public sealed class ComparisonQuery
{
    public DateTime StartDate1 { get; set; }
    public DateTime EndDate1 { get; set; }
    public DateTime StartDate2 { get; set; }
    public DateTime EndDate2 { get; set; }
}

/// <summary>Sales Intelligence sayfasının tüm verisi; tek istekte, seçili tarih aralığı için döner.</summary>
public sealed class AnalyticsOverviewDto
{
    /// <summary>Uygulanan aralık (başlangıç dahil, bitiş hariç).</summary>
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Karşılaştırma için kullanılan önceki dönem (başlangıç dahil, bitiş hariç).</summary>
    public DateTime CompareStartDate { get; set; }
    public DateTime CompareEndDate { get; set; }

    public IReadOnlyList<TopCustomerDto> TopCustomers { get; set; } = Array.Empty<TopCustomerDto>();
    public IReadOnlyList<TopCityDto> TopCities { get; set; } = Array.Empty<TopCityDto>();
    public IReadOnlyList<TopProductDto> TopProducts { get; set; } = Array.Empty<TopProductDto>();
    public IReadOnlyList<CategoryMonthlyDto> CategoryMonthly { get; set; } = Array.Empty<CategoryMonthlyDto>();
    public RepeatCustomerRateDto RepeatRate { get; set; } = new();
    public IReadOnlyList<PeriodComparisonDto> Comparison { get; set; } = Array.Empty<PeriodComparisonDto>();
}