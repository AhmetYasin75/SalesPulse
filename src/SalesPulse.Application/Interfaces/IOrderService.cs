using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>Order Explorer iş kuralları: doğrulama, sınırlar ve repository çağrıları.</summary>
public interface IOrderService
{
    /// <summary>Filtreli, sıralı ve sayfalı sipariş listesi. Geçersiz filtrede ArgumentException fırlatır.</summary>
    Task<PagedResult<OrderListItemDto>> GetOrdersAsync(
        OrderQuery query, CancellationToken cancellationToken = default);

    /// <summary>Sipariş detayı. Geçersiz numara veya kayıt yoksa null döner.</summary>
    Task<OrderDetailDto?> GetOrderDetailAsync(
        int orderId, CancellationToken cancellationToken = default);

    /// <summary>CSV için sınırlı sayıda sipariş (en fazla MaxExportRows).</summary>
    Task<IReadOnlyList<OrderListItemDto>> GetExportRowsAsync(
        OrderQuery query, CancellationToken cancellationToken = default);

    /// <summary>Şehir filtresi için 81 şehrin listesi.</summary>
    Task<IReadOnlyList<CityDto>> GetCitiesAsync(CancellationToken cancellationToken = default);
}