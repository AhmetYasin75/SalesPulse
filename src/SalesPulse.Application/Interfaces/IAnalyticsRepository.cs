using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>
/// Sales Intelligence için veri erişim sözleşmesi. Uygulaması Infrastructure katmanındadır
/// (Dapper + saklı yordamlar). Tüm toplulaştırma SQL Server'da yapılır.
/// Tarih aralığı: StartDate dahil, EndDate hariç.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>Ciroya göre ilk 20 müşteri (usp_GetTopCustomers).</summary>
    Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
        DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>Ciroya göre ilk topN kategorinin aylık performansı (usp_GetCategoryMonthlyPerformance).</summary>
    Task<IReadOnlyList<CategoryMonthlyDto>> GetCategoryMonthlyAsync(
        DashboardQuery query, int topN, CancellationToken cancellationToken = default);

    /// <summary>Tekrar alışveriş yapan müşteri oranı (usp_GetRepeatCustomerRate).</summary>
    Task<RepeatCustomerRateDto> GetRepeatRateAsync(
        DashboardQuery query, CancellationToken cancellationToken = default);

    /// <summary>İki tarih aralığının karşılaştırması; Donem 1 ve Donem 2 olmak üzere 2 satır (usp_GetDateRangeComparison).</summary>
    Task<IReadOnlyList<PeriodComparisonDto>> GetComparisonAsync(
        ComparisonQuery query, CancellationToken cancellationToken = default);
}