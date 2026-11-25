using System.Data;
using Microsoft.Data.SqlClient;
using SalesPulse.Application.Interfaces;

namespace SalesPulse.Infrastructure.Data;

/// <summary>
/// SQL Server için bağlantı fabrikası. Bağlantı dizesi dışarıdan (yapılandırmadan) verilir.
/// </summary>
public sealed class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Bağlantı dizesi boş olamaz.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            // Async açılır; hata olursa bağlantı sızmasın diye kapatılır.
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}