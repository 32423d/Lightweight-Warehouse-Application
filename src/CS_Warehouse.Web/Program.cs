using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register MVC controllers and Razor views.
builder.Services.AddControllersWithViews();

// Use SQLite. Read the connection string from configuration when it exists.
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Warehouse")
        ?? "Data Source=stockroom-lite.db"));

// Inject the system clock so tests can replace it with a fixed time.
builder.Services.AddSingleton(TimeProvider.System);

// Create one inventory service for each HTTP request.
builder.Services.AddScoped<IInventoryService, InventoryService>();

var app = builder.Build();

// Apply pending migrations before the application accepts requests.
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (!app.Environment.IsDevelopment())
{
    // Hide detailed errors from users in production.
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Send unknown paths to the dashboard.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
