namespace SalesPulse.Application.Models;

/// <summary>Şehir filtresi için şehir (plaka kodu + ad). usp_GetCities sonucuna karşılık gelir.</summary>
public sealed class CityDto
{
    public int CityId { get; set; }
    public string CityName { get; set; } = string.Empty;
}