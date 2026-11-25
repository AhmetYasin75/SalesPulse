using Microsoft.AspNetCore.Mvc;
using SalesPulse.Application.Interfaces;

namespace SalesPulse.Web.Controllers;

/// <summary>Sales Intelligence: gelişmiş satış analizi sayfası ve verileri.</summary>
public class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>Satış Analizi sayfası (arayüz). Veri /Analytics/Data ucundan gelir.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Tüm analiz verisi (tek istek, JSON). Tarihler boşsa son 12 ay kullanılır.
    /// Karşılaştırma dönemi, seçili aralıktan hemen önceki aynı uzunluktaki aralıktır.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Data(
        DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken)
    {
        try
        {
            var overview = await _analyticsService.GetOverviewAsync(startDate, endDate, cancellationToken);
            return Json(overview);
        }
        catch (ArgumentException ex)
        {
            // Geçersiz tarih aralığı: kullanıcıya anlaşılır hata
            return BadRequest(new { error = ex.Message });
        }
    }
}