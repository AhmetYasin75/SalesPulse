using System.Data;
using Dapper;
using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Infrastructure.Repositories;

/// <summary>
/// Order Explorer veri erişimi: saklı yordamlar Dapper ile async ve parametrik çağrılır.
/// Filtreleme, sıralama ve sayfalama SQL Server'da yapılır; C# yalnızca sonucu okur.
/// </summary>
public sealed class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<OrderListItemDto>> GetOrdersAsync(
        OrderQuery query, CancellationToken cancellationToken = default)
    {
        // SQL tarafıyla aynı sınırlar (kullanıcıya gerçek değerler dönsün)
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, 100);

        var parameters = CreateFilterParameters(query);
        parameters.Add("@SortBy", query.SortBy, DbType.String, size: 30);
        parameters.Add("@SortDir", query.SortDir, DbType.String, size: 4);
        parameters.Add("@PageNumber", pageNumber, DbType.Int32);
        parameters.Add("@PageSize", pageSize, DbType.Int32);

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.usp_GetOrders", parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // 1. küme: sayfadaki satırlar, 2. küme: toplam kayıt sayısı
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<OrderListItemDto>()).ToList();
        var totalCount = await grid.ReadSingleAsync<int>();

        return new PagedResult<OrderListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetailDto?> GetOrderDetailAsync(
        int orderId, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OrderId", orderId, DbType.Int32);

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.usp_GetOrderDetail", parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        // 1. küme: sipariş başlığı (yoksa boş), 2. küme: kalemler
        using var grid = await connection.QueryMultipleAsync(command);
        var header = await grid.ReadFirstOrDefaultAsync<OrderDetailDto>();
        var items = (await grid.ReadAsync<OrderDetailItemDto>()).ToList();

        if (header is null)
            return null;

        header.Kalemler = items;
        return header;
    }

    public async Task<IReadOnlyList<OrderListItemDto>> ExportOrdersAsync(
        OrderQuery query, int maxRows, CancellationToken cancellationToken = default)
    {
        var parameters = CreateFilterParameters(query);
        parameters.Add("@MaxRows", maxRows, DbType.Int32);

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.usp_ExportOrders", parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<OrderListItemDto>(command);
        return rows.ToList();
    }

    public async Task<IReadOnlyList<CityDto>> GetCitiesAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.usp_GetCities",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<CityDto>(command);
        return rows.ToList();
    }

    /// <summary>Sayfa ve CSV sorgularında ortak olan filtre parametrelerini hazırlar.</summary>
    private static DynamicParameters CreateFilterParameters(OrderQuery query)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Search", query.Search, DbType.String, size: 100);
        parameters.Add("@StartDate", query.StartDate, DbType.DateTime2);
        parameters.Add("@EndDate", query.EndDate, DbType.DateTime2);
        parameters.Add("@CityId", query.CityId, DbType.Int32);
        parameters.Add("@Status", query.Status, DbType.String, size: 20);
        parameters.Add("@MinAmount", query.MinAmount, DbType.Decimal);
        parameters.Add("@MaxAmount", query.MaxAmount, DbType.Decimal);
        return parameters;
    }
}