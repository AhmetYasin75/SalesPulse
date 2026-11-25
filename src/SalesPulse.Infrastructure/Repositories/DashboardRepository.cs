using System.Data;
using Dapper;
using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Infrastructure.Repositories;

/// <summary>
/// Dashboard veri erişimi: saklı yordamlar Dapper ile async ve parametrik çağrılır.
/// Toplulaştırma SQL Server'da yapılır; C# yalnızca küçük sonuç kümelerini okur.
/// </summary>
public sealed class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardKpiDto> GetKpisAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = CreateCommand("dbo.usp_GetDashboardKpis", DateParameters(query), cancellationToken);

        // Toplulaştırma tek satır döndürür
        return await connection.QuerySingleAsync<DashboardKpiDto>(command);
    }

    public Task<IReadOnlyList<MonthlyTrendDto>> GetMonthlyTrendAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
        => QueryListAsync<MonthlyTrendDto>("dbo.usp_GetMonthlySalesTrend", DateParameters(query), cancellationToken);

    public Task<IReadOnlyList<TopCityDto>> GetTopCitiesAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
        => QueryListAsync<TopCityDto>("dbo.usp_GetTopCities", DateParameters(query), cancellationToken);

    public Task<IReadOnlyList<TopCategoryDto>> GetTopCategoriesAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
        => QueryListAsync<TopCategoryDto>("dbo.usp_GetTopCategories", DateParameters(query), cancellationToken);

    public Task<IReadOnlyList<StatusDistributionDto>> GetStatusDistributionAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
        => QueryListAsync<StatusDistributionDto>("dbo.usp_GetOrderStatusDistribution", DateParameters(query), cancellationToken);

    public Task<IReadOnlyList<TopProductDto>> GetTopProductsAsync(
        DashboardQuery query, string sortBy, CancellationToken cancellationToken = default)
    {
        var parameters = DateParameters(query);
        parameters.Add("@SortBy", sortBy, DbType.String, size: 10);
        return QueryListAsync<TopProductDto>("dbo.usp_GetTopProducts", parameters, cancellationToken);
    }

    /// <summary>Tüm dashboard yordamlarının ortak tarih parametreleri.</summary>
    private static DynamicParameters DateParameters(DashboardQuery query)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StartDate", query.StartDate, DbType.DateTime2);
        parameters.Add("@EndDate", query.EndDate, DbType.DateTime2);
        return parameters;
    }

    private static CommandDefinition CreateCommand(
        string procedure, DynamicParameters parameters, CancellationToken cancellationToken)
        => new(procedure, parameters,
               commandType: CommandType.StoredProcedure,
               cancellationToken: cancellationToken);

    private async Task<IReadOnlyList<T>> QueryListAsync<T>(
        string procedure, DynamicParameters parameters, CancellationToken cancellationToken)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<T>(CreateCommand(procedure, parameters, cancellationToken));
        return rows.ToList();
    }
}