using Microsoft.Extensions.DependencyInjection;
using SalesPulse.Application.Interfaces;
using SalesPulse.Infrastructure.Data;
using SalesPulse.Infrastructure.Repositories;

namespace SalesPulse.Infrastructure;

/// <summary>Infrastructure katmanının servis kayıtları.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Veri erişim servislerini kaydeder. Bağlantı dizesi Web projesinden (appsettings.json) verilir.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, string connectionString)
    {
        // Fabrika durumsuzdur (yalnızca bağlantı dizesini tutar): tek örnek yeterli.
        services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));

        // Repository'ler her istekte yeni oluşturulur (bağlantıyı kendileri açıp kapatır).
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

        return services;
    }
}