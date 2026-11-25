namespace SalesPulse.Application.Models;

/// <summary>
/// Order Explorer isteği: arama, filtreler, sıralama ve sayfalama seçimleri.
/// Özellikler usp_GetOrders / usp_ExportOrders parametrelerine karşılık gelir.
/// Boş (null) bırakılan filtre uygulanmaz.
/// </summary>
public sealed class OrderQuery
{
    /// <summary>Sipariş no (sayı) veya müşteri adı soyadı (başlangıç eşleşmesi).</summary>
    public string? Search { get; set; }

    /// <summary>Tarih başlangıcı (dahil).</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Tarih bitişi (hariç).</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Şehir (plaka kodu, 1-81).</summary>
    public int? CityId { get; set; }

    /// <summary>Sipariş durumu (ör. "Tamamlandı", "Beklemede").</summary>
    public string? Status { get; set; }

    /// <summary>En düşük tutar.</summary>
    public decimal? MinAmount { get; set; }

    /// <summary>En yüksek tutar.</summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Sıralanacak kolon: SiparisNo, Musteri, Sehir, Tarih, Tutar, OdemeYontemi, Durum.
    /// Geçersiz değer SQL tarafında varsayılana (Tarih) düşer.
    /// </summary>
    public string SortBy { get; set; } = "Tarih";

    /// <summary>Sıralama yönü: "ASC" veya "DESC".</summary>
    public string SortDir { get; set; } = "DESC";

    /// <summary>Sayfa numarası (1'den başlar).</summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>Sayfa boyutu (SQL tarafı 1-100 aralığına sınırlar).</summary>
    public int PageSize { get; set; } = 20;
}