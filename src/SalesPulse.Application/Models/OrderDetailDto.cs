namespace SalesPulse.Application.Models;

/// <summary>
/// Sipariş detay panelinin üst bilgisi (usp_GetOrderDetail 1. sonuç kümesi) ve kalemleri.
/// </summary>
public sealed class OrderDetailDto
{
    public int SiparisNo { get; set; }
    public string Musteri { get; set; } = string.Empty;
    public string Eposta { get; set; } = string.Empty;
    public string Sehir { get; set; } = string.Empty;
    public DateTime Tarih { get; set; }
    public decimal Tutar { get; set; }
    public string OdemeYontemi { get; set; } = string.Empty;
    public string Durum { get; set; } = string.Empty;

    /// <summary>Siparişin kalemleri (2. sonuç kümesi). Repository tarafından doldurulur.</summary>
    public IReadOnlyList<OrderDetailItemDto> Kalemler { get; set; } = Array.Empty<OrderDetailItemDto>();
}

/// <summary>Tek bir sipariş kalemi (usp_GetOrderDetail 2. sonuç kümesi).</summary>
public sealed class OrderDetailItemDto
{
    public int KalemNo { get; set; }
    public string Urun { get; set; } = string.Empty;
    public string Kategori { get; set; } = string.Empty;
    public int Adet { get; set; }
    public decimal BirimFiyat { get; set; }
    public decimal SatirToplami { get; set; }
}