using System.Data;

namespace SalesPulse.Application.Interfaces;

/// <summary>
/// Veritabanı bağlantısı üreten fabrika.
/// Her çağrı yeni bir bağlantı döndürür; çağıran taraf bağlantıyı kullanıp Dispose etmelidir.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>Bağlantıyı açıp (async) hazır halde döndürür.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}