using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Application.Services;

/// <summary>
/// Dashboard iş kurallarını uygular. Tarih aralığını doğrular, varsayılanı belirler ve
/// birbirinden bağımsız sorguları paralel çalıştırır. Toplulaştırma SQL Server'dadır.
/// </summary>
public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _repository;

    public DashboardService(IDashboardRepository repository)
    {
        _repository = repository;
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(
        DateTime? startDate, DateTime? endDate, string? productSort,
        CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(startDate, endDate);
        var sortBy = string.Equals(productSort, "Adet", StringComparison.OrdinalIgnoreCase) ? "Adet" : "Ciro";

        // Altı sorgu birbirinden bağımsız ve her biri kendi bağlantısını açar; paralel çalışabilir
        var kpis = _repository.GetKpisAsync(query, cancellationToken);
        var trend = _repository.GetMonthlyTrendAsync(query, cancellationToken);
        var cities = _repository.GetTopCitiesAsync(query, cancellationToken);
        var categories = _repository.GetTopCategoriesAsync(query, cancellationToken);
        var statuses = _repository.GetStatusDistributionAsync(query, cancellationToken);
        var products = _repository.GetTopProductsAsync(query, sortBy, cancellationToken);

        await Task.WhenAll(kpis, trend, cities, categories, statuses, products);

        return new DashboardOverviewDto
        {
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            Kpis = kpis.Result,
            MonthlyTrend = trend.Result,
            TopCities = cities.Result,
            TopCategories = categories.Result,
            StatusDistribution = statuses.Result,
            TopProducts = products.Result
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