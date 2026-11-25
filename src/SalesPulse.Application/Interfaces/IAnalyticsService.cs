using SalesPulse.Application.Models;

namespace SalesPulse.Application.Interfaces;

/// <summary>Sales Intelligence iş kuralları: tarih aralığı doğrulama, karşılaştırma dönemi ve verinin toplanması.</summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Seçili tarih aralığı için tüm analiz verisini getirir. startDate/endDate boşsa son 12 ay kullanılır.
    /// endDate gün dahil kabul edilir. Karşılaştırma dönemi, seçili aralıktan hemen önceki aynı uzunluktaki aralıktır.
    /// Geçersiz aralıkta ArgumentException fırlatır.
    /// </summary>
    Task<AnalyticsOverviewDto> GetOverviewAsync(
        DateTime? startDate, DateTime? endDate,
        CancellationToken cancellationToken = default);
}