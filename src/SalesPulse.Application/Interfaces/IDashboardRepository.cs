using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>
/// Dashboard için veri erişim sözleşmesi. Uygulaması Infrastructure katmanındadır
/// (Dapper + saklı yordamlar). Tüm toplulaştırma SQL Server'da yapılır.
/// Tarih aralığı: StartDate dahil, EndDate hariç.
/// </summary>
public interface IDashboardRepository
{
    /// <summary>Toplam sipariş, ciro, müşteri ve ortalama sepet (usp_GetDashboardKpis).</summary>
    Task<DashboardKpiDto> GetKpisAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>Aylık sipariş sayısı ve satış tutarı (usp_GetMonthlySalesTrend).</summary>
    Task<IReadOnlyList<MonthlyTrendDto>> GetMonthlyTrendAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>Ciroya göre ilk 10 şehir (usp_GetTopCities).</summary>
    Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>Ciroya göre ilk 10 kategori (usp_GetTopCategories).</summary>
    Task<IReadOnlyList<TopCategoryDto>> GetTopCategoriesAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>Sipariş durumu dağılımı (usp_GetOrderStatusDistribution).</summary>
    Task<IReadOnlyList<StatusDistributionDto>> GetStatusDistributionAsync(DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>İlk 10 ürün; sortBy "Ciro" veya "Adet" (usp_GetTopProducts).</summary>
    Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(DashboardQuery query, string sortBy, CancellationToken cancellationToken = default);
}