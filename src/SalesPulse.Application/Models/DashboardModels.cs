namespace SalesPulse.Application.Models;

// Dashboard için veri modelleri. Özellik adları saklı yordamların kolon adlarıyla
// birebir aynıdır; Dapper eşlemeyi ada göre yapar.

/// <summary>Dashboard tarih filtresi. Bitiş tarihi SQL'de dışarıda tutulur (< EndDate).</summary>
public sealed class DashboardQuery
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

/// <summary>Üst kartlar: toplam sipariş, ciro, müşteri ve ortalama sepet (usp_GetDashboardKpis).</summary>
public sealed class DashboardKpiDto
{
    public int ToplamSiparis { get; set; }
    public decimal ToplamCiro { get; set; }
    public int ToplamMusteri { get; set; }
    public decimal OrtalamaSepet { get; set; }
}

/// <summary>Aylık satış trendi, bir ay için bir satır (usp_GetMonthlySalesTrend).</summary>
public sealed class MonthlyTrendDto
{
    public int Yil { get; set; }
    public int Ay { get; set; }
    public int SiparisSayisi { get; set; }
    public decimal ToplamSatis { get; set; }
}

/// <summary>En çok satan şehirler (usp_GetTopCities).</summary>
public sealed class TopCityDto
{
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
    public decimal ToplamCiro { get; set; }
}

/// <summary>En çok satan kategoriler (usp_GetTopCategories).</summary>
public sealed class TopCategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int SatisAdedi { get; set; }
    public decimal ToplamCiro { get; set; }
}

/// <summary>Sipariş durumu dağılımı (usp_GetOrderStatusDistribution).</summary>
public sealed class StatusDistributionDto
{
    public string Durum { get; set; } = string.Empty;
    public int SiparisSayisi { get; set; }
    public decimal Yuzde { get; set; }
}

/// <summary>En çok satan ürünler (usp_GetTopProducts).</summary>
public sealed class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int SatisAdedi { get; set; }
    public decimal ToplamCiro { get; set; }
}