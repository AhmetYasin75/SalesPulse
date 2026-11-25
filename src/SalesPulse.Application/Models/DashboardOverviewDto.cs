namespace SalesPulse.Application.Models;

/// <summary>Dashboard sayfasının tüm verisi; tek istekte, seçili tarih aralığı için döner.</summary>
public sealed class DashboardOverviewDto
{
    /// <summary>Uygulanan başlangıç tarihi (dahil).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Uygulanan bitiş tarihi (hariç; SQL'deki sınırla aynı).</summary>
    public DateTime EndDate { get; set; }

    public DashboardKpiDto Kpis { get; set; } = new();
    public IReadOnlyList<MonthlyTrendDto> MonthlyTrend { get; set; } = Array.Empty<MonthlyTrendDto>();
    public IReadOnlyList<TopCityDto> TopCities { get; set; } = Array.Empty<TopCityDto>();
    public IReadOnlyList<TopCategoryDto> TopCategories { get; set; } = Array.Empty<TopCategoryDto>();
    public IReadOnlyList<StatusDistributionDto> StatusDistribution { get; set; } = Array.Empty<StatusDistributionDto>();
    public IReadOnlyList<TopProductDto> TopProducts { get; set; } = Array.Empty<TopProductDto>();
}