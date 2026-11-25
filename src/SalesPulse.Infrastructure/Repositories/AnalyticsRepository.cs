using System.Data;
using Dapper;
using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Infrastructure.Repositories;

/// <summary>
/// Sales Intelligence veri erişimi: saklı yordamlar Dapper ile async ve parametrik çağrılır.
/// JOIN, GROUP BY ve HAVING işlemleri SQL Server'da yapılır; C# yalnızca küçük sonuç kümelerini okur.
/// </summary>
public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AnalyticsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
        => QueryListAsync<TopCustomerDto>("dbo.usp_GetTopCustomers", DateParameters(query), cancellationToken);

    public Task<IReadOnlyList<CategoryMonthlyDto>> GetCategoryMonthlyAsync(
        DashboardQuery query, int topN, CancellationToken cancellationToken = default)
    {
        // @TopN yordamda varsayılan değerli değil; her çağrıda açıkça gönderilir
        var parameters = DateParameters(query);
        parameters.Add("@TopN", topN, DbType.Int32);
        return QueryListAsync<CategoryMonthlyDto>("dbo.usp_GetCategoryMonthlyPerformance", parameters, cancellationToken);
    }

    public async Task<RepeatCustomerRateDto> GetRepeatRateAsync(
        DashboardQuery query, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var command = CreateCommand("dbo.usp_GetRepeatCustomerRate", DateParameters(query), cancellationToken);

        // Toplulaştırma tek satır döndürür
        return await connection.QuerySingleAsync<RepeatCustomerRateDto>(command);
    }

    public Task<IReadOnlyList<PeriodComparisonDto>> GetComparisonAsync(
        ComparisonQuery query, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@StartDate1", query.StartDate1, DbType.DateTime2);
        parameters.Add("@EndDate1", query.EndDate1, DbType.DateTime2);
        parameters.Add("@StartDate2", query.StartDate2, DbType.DateTime2);
        parameters.Add("@EndDate2", query.EndDate2, DbType.DateTime2);
        return QueryListAsync<PeriodComparisonDto>("dbo.usp_GetDateRangeComparison", parameters, cancellationToken);
    }

    /// <summary>Tek aralık alan yordamların ortak tarih parametreleri.</summary>
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