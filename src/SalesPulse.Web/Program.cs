using SalesPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
// Veri eriþim katmaný: baðlantý dizesi appsettings.json'dan okunur
var connectionString = builder.Configuration.GetConnectionString("SalesPulseDb")
    ?? throw new InvalidOperationException("Baðlantý dizesi 'SalesPulseDb' appsettings.json içinde bulunamadý.");

builder.Services.AddInfrastructure(connectionString);
// Uygulama katmaný servisleri
builder.Services.AddScoped<SalesPulse.Application.Interfaces.IOrderService,
                           SalesPulse.Application.Services.OrderService>();
builder.Services.AddScoped<SalesPulse.Application.Interfaces.IDashboardService,
                           SalesPulse.Application.Services.DashboardService>();
builder.Services.AddScoped<SalesPulse.Application.Interfaces.IAnalyticsService,
                           SalesPulse.Application.Services.AnalyticsService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
