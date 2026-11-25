using Microsoft.AspNetCore.Mvc;
using SalesPulse.Application.Interfaces;

namespace SalesPulse.Web.Controllers;

/// <summary>Dashboard: seçili tarih aralığı için KPI, trend ve dağılım verilerini JSON olarak sunar.</summary>
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>Dashboard sayfası (arayüz). Veri /Dashboard/Data ucundan gelir.</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>
    /// Tüm dashboard verisi (tek istek). Tarihler boşsa son 12 ay kullanılır.
    /// productSort: "Ciro" veya "Adet".
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Data(
        DateTime? startDate, DateTime? endDate, string? productSort,
        CancellationToken cancellationToken)
    {
        try
        {
            var overview = await _dashboardService.GetOverviewAsync(
                startDate, endDate, productSort, cancellationToken);

            return Json(overview);
        }
        catch (ArgumentException ex)
        {
            // Geçersiz tarih aralığı: kullanıcıya anlaşılır hata
            return BadRequest(new { error = ex.Message });
        }
    }
}