namespace SalesPulse.Application.Models;

/// <summary>
/// Sunucu taraflı sayfalamanın sonucu: o sayfadaki satırlar + filtreye uyan toplam kayıt sayısı.
/// </summary>
/// <typeparam name="T">Satır tipi (ör. OrderListItemDto).</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>Geçerli sayfadaki satırlar.</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>Filtreye uyan toplam kayıt sayısı (tüm sayfalar).</summary>
    public int TotalCount { get; init; }

    /// <summary>Geçerli sayfa numarası (1'den başlar).</summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>Sayfa başına satır sayısı.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>Toplam sayfa sayısı (yukarı yuvarlanır).</summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}