using Microsoft.AspNetCore.Mvc;
using SalesPulse.Application.Interfaces;
using SalesPulse.Application.Models;
using SalesPulse.Application.Services;

namespace SalesPulse.Web.Controllers;

/// <summary>Order Explorer: sipariş listesi, detay, şehir listesi ve CSV uçları.</summary>
public class OrdersController : Controller
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Order Explorer sayfası (tablo arayüzü).</summary>
    [HttpGet]
    public IActionResult Index() => View();

    /// <summary>Filtreli, sıralı, sayfalı sipariş listesi (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Data([FromQuery] OrderQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orderService.GetOrdersAsync(query, cancellationToken);

            return Json(new
            {
                items = result.Items,
                totalCount = result.TotalCount,
                pageNumber = result.PageNumber,
                pageSize = result.PageSize,
                totalPages = result.TotalPages
            });
        }
        catch (ArgumentException ex)
        {
            // Geçersiz filtre (ör. başlangıç > bitiş): kullanıcıya anlaşılır hata
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Şehir filtresi için 81 şehrin listesi (JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Cities(CancellationToken cancellationToken)
    {
        var cities = await _orderService.GetCitiesAsync(cancellationToken);
        return Json(cities);
    }

    /// <summary>Tek siparişin başlığı ve kalemleri (detay modalı için JSON).</summary>
    [HttpGet]
    public async Task<IActionResult> Detail(int id, CancellationToken cancellationToken)
    {
        var detail = await _orderService.GetOrderDetailAsync(id, cancellationToken);
        if (detail is null)
            return NotFound(new { error = "Sipariş bulunamadı." });

        return Json(detail);
    }

    /// <summary>Filtreye uyan siparişleri en fazla 50.000 satırla CSV olarak indirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportCsv([FromQuery] OrderQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _orderService.GetExportRowsAsync(query, cancellationToken);
            var bytes = OrderCsvBuilder.Build(rows);
            var fileName = $"siparisler-{DateTime.Now:yyyyMMdd-HHmm}.csv";

            return File(bytes, "text/csv; charset=utf-8", fileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}