using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>Dashboard iş kuralları: tarih aralığı doğrulama/varsayılanı ve verinin toplanması.</summary>
public interface IDashboardService
{
    /// <summary>
    /// Seçili tarih aralığı için tüm dashboard verisini getirir.
    /// startDate/endDate boşsa son 12 ay kullanılır. endDate gün dahil kabul edilir.
    /// productSort: "Ciro" veya "Adet". Geçersiz aralıkta ArgumentException fırlatır.
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(
        DateTime? startDate, DateTime? endDate, string? productSort,
        CancellationToken cancellationToken = default);
}