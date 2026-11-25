using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>
/// Order Explorer için veri erişim sözleşmesi.
/// Uygulaması Infrastructure katmanında (Dapper + saklı yordamlar) yer alır.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Filtre, sıralama ve sayfalamaya göre siparişleri ve toplam kayıt sayısını getirir
    /// (usp_GetOrders).
    /// </summary>
    Task<PagedResult<OrderListItemDto>> GetOrdersAsync(
        OrderQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tek siparişin başlığını ve kalemlerini getirir (usp_GetOrderDetail).
    /// Sipariş yoksa null döner.
    /// </summary>
    Task<OrderDetailDto?> GetOrderDetailAsync(
        int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Filtreye uyan siparişleri CSV için getirir (usp_ExportOrders).
    /// Üst sınır SQL tarafında da uygulanır (en fazla 50.000 satır).
    /// </summary>
    Task<IReadOnlyList<OrderListItemDto>> ExportOrdersAsync(
        OrderQuery query, int maxRows, CancellationToken cancellationToken = default);

    /// <summary>Şehir filtresi için 81 şehrin listesini getirir (usp_GetCities).</summary>
    Task<IReadOnlyList<CityDto>> GetCitiesAsync(CancellationToken cancellationToken = default);
}