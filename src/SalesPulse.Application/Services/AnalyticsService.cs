using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Application.Services;

/// <summary>
/// Sales Intelligence iş kurallarını uygular. Tarih aralığını doğrular, önceki dönemi hesaplar ve
/// birbirinden bağımsız sorguları paralel çalıştırır. Toplulaştırma SQL Server'dadır.
/// </summary>
public sealed class AnalyticsService : IAnalyticsService
{
    /// <summary>Kategori aylık performansında gösterilen kategori sayısı.</summary>
    private const int CategoryMonthlyTopN = 5;

    /// <summary>Gelir sıralamasında kullanılan ürün ölçütü ("en yüksek gelir sağlayan ürünler").</summary>
    private const string ProductSortBy = "Ciro";

    private readonly IAnalyticsRepository _analytics;
    private readonly IDashboardRepository _dashboard; // şehir ve ürün sıralamaları Dashboard yordamlarını yeniden kullanır

    public AnalyticsService(IAnalyticsRepository analytics, IDashboardRepository dashboard)
    {
        _analytics = analytics;
        _dashboard = dashboard;
    }

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(startDate, endDate);

        // Önceki dönem: aynı uzunlukta, seçili aralıktan hemen önce biten aralık
        var length = query.EndDate - query.StartDate;
        var comparison = new ComparisonQuery
        {
            StartDate1 = query.StartDate,
            EndDate1 = query.EndDate,
            StartDate2 = query.StartDate - length,
            EndDate2 = query.StartDate
        };

        // Beş sorgu birbirinden bağımsız ve her biri kendi bağlantısını açar; paralel çalışabilir
        var customers = _analytics.GetTopCustomersAsync(query, cancellationToken);
        var cities = _dashboard.GetTopCitiesAsync(query, cancellationToken);
        var products = _dashboard.GetTopProductsAsync(query, ProductSortBy, cancellationToken);
        var categoryMonthly = _analytics.GetCategoryMonthlyAsync(query, CategoryMonthlyTopN, cancellationToken);
        var repeatRate = _analytics.GetRepeatRateAsync(query, cancellationToken);
        var periods = _analytics.GetComparisonAsync(comparison, cancellationToken);

        await Task.WhenAll(customers, cities, products, categoryMonthly, repeatRate, periods);

        return new AnalyticsOverviewDto
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            CompareStartDate = comparison.StartDate2,
            CompareEndDate = comparison.EndDate2,
            TopCustomers = customers.Result,
            TopCities = cities.Result,
            TopProducts = products.Result,
            CategoryMonthly = categoryMonthly.Result,
            RepeatRate = repeatRate.Result,
            Comparison = periods.Result
        };
    }

    /// <summary>
    /// Tarih aralığını hazırlar. Boşsa: içinde bulunulan ay dahil son 12 ay.
    /// Kullanıcının seçtiği bitiş günü dahildir; SQL'de "hariç" sınır için ertesi güne çevrilir.
    /// </summary>
    private static DashboardQuery BuildQuery(DateTime? startDate, DateTime? endDate)
    {
        var today = DateTime.Today;

        var start = startDate?.Date
            ?? new DateTime(today.Year, today.Month, 1).AddMonths(-11);
        var endInclusive = endDate?.Date ?? today;

        if (start > endInclusive)
            throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        return new DashboardQuery
        {
            StartDate = start,
            EndDate = endInclusive.AddDays(1)
        };
    }
}