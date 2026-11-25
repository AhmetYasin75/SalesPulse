namespace SalesPulse.Application.Models;

/// <summary>
/// Order Explorer tablosundaki tek bir sipariş satırı.
/// Özellik adları usp_GetOrders'ın döndürdüğü kolon adlarıyla birebir aynıdır (Dapper eşlemesi için).
/// </summary>
public sealed class OrderListItemDto
{
    public int SiparisNo { get; set; }
    public string Musteri { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public decimal Tutar { get; set; }
    public string OdemeYontemi { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;
}