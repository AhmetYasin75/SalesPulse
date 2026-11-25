using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;

namespace SalesPulse.Application.Services;

/// <summary>Order Explorer iş kurallarını uygular; veri erişimini IOrderRepository'ye bırakır.</summary>
public sealed class OrderService : IOrderService
{
    /// <summary>CSV dışa aktarma üst sınırı (SQL tarafındaki tavanla aynı).</summary>
    public const int MaxExportRows = 50_000;

    private readonly IOrderRepository _repository;

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }

    public Task<PagedResult<OrderListItemDto>> GetOrdersAsync(
        OrderQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        return _repository.GetOrdersAsync(normalized, cancellationToken);
    }

    public Task<OrderDetailDto?> GetOrderDetailAsync(
        int orderId, CancellationToken cancellationToken = default)
    {
        if (orderId <= 0)
            return Task.FromResult<OrderDetailDto?>(null);

        return _repository.GetOrderDetailAsync(orderId, cancellationToken);
    }

    public Task<IReadOnlyList<OrderListItemDto>> GetExportRowsAsync(
        OrderQuery query, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(query);
        return _repository.ExportOrdersAsync(normalized, MaxExportRows, cancellationToken);
    }

    public Task<IReadOnlyList<CityDto>> GetCitiesAsync(CancellationToken cancellationToken = default)
        => _repository.GetCitiesAsync(cancellationToken);

    /// <summary>Filtreleri temizler ve doğrular; çağıranın nesnesini değiştirmez, kopya döndürür.</summary>
    private static OrderQuery Normalize(OrderQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.StartDate.HasValue && query.EndDate.HasValue && query.StartDate > query.EndDate)
            throw new ArgumentException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");

        if (query.MinAmount.HasValue && query.MaxAmount.HasValue && query.MinAmount > query.MaxAmount)
            throw new ArgumentException("En düşük tutar en yüksek tutardan büyük olamaz.");

        if (query.CityId.HasValue && (query.CityId < 1 || query.CityId > 81))
            throw new ArgumentException("Şehir kodu 1 ile 81 arasında olmalıdır.");

        return new OrderQuery
        {
            Search = NullIfBlank(query.Search),
            StartDate = query.StartDate,
            EndDate = query.EndDate,
            CityId = query.CityId,
            Status = NullIfBlank(query.Status),
            MinAmount = query.MinAmount,
            MaxAmount = query.MaxAmount,
            SortBy = string.IsNullOrWhiteSpace(query.SortBy) ? "Tarih" : query.SortBy.Trim(),
            SortDir = string.Equals(query.SortDir, "ASC", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC",
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    private static string? NullIfBlank(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}