using CS_Warehouse.Web.Controllers;
using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Models;
using CS_Warehouse.Web.Services;
using CS_Warehouse.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Tests;

/// <summary>
/// Verifies inventory rules, database constraints, and dashboard totals.
/// The tests use SQLite in memory so foreign keys and indexes stay active.
/// </summary>
public sealed class InventoryServiceTests
{
    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 17, 18, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Receive_updates_balance_and_writes_audit_record()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        // Seeded quantity for product 1 at MAIN is 7. A receipt of 3 must become 10.
        var movement = await service.RecordMovementAsync(
            productId: 1,
            locationId: 1,
            StockMovementType.Receive,
            quantity: 3,
            note: "  Supplier delivery  ");

        var balance = await database.Context.InventoryBalances
            .SingleAsync(item => item.ProductId == 1 && item.LocationId == 1);

        Assert.Equal(10, balance.QuantityOnHand);
        Assert.Equal(FixedNow.UtcDateTime, balance.UpdatedAtUtc);
        Assert.Equal(3, movement.QuantityChange);
        Assert.Equal("Supplier delivery", movement.Note);
        Assert.Equal(FixedNow.UtcDateTime, movement.OccurredAtUtc);
    }

    [Fact]
    public async Task Issue_reduces_available_stock()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        // Seeded quantity for product 2 is 45. An issue of 5 must become 40.
        var movement = await service.RecordMovementAsync(
            productId: 2,
            locationId: 1,
            StockMovementType.Issue,
            quantity: 5,
            note: "Order 1042");

        var balance = await database.Context.InventoryBalances
            .SingleAsync(item => item.ProductId == 2 && item.LocationId == 1);

        Assert.Equal(40, balance.QuantityOnHand);
        Assert.Equal(-5, movement.QuantityChange);
        Assert.Equal(StockMovementType.Issue, movement.Type);
    }

    [Fact]
    public async Task Issue_rejects_insufficient_stock_without_partial_changes()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);
        var movementCountBefore = await database.Context.StockMovements.CountAsync();

        // Product 1 has 7 units. An issue of 8 must fail.
        var exception = await Assert.ThrowsAsync<InventoryOperationException>(
            () => service.RecordMovementAsync(
                productId: 1,
                locationId: 1,
                StockMovementType.Issue,
                quantity: 8,
                note: null));

        // Clear tracked values before the test reads the database again.
        database.Context.ChangeTracker.Clear();
        var balance = await database.Context.InventoryBalances
            .SingleAsync(item => item.ProductId == 1 && item.LocationId == 1);
        var movementCountAfter = await database.Context.StockMovements.CountAsync();

        Assert.Contains("Only 7", exception.Message);
        Assert.Equal(7, balance.QuantityOnHand);
        Assert.Equal(movementCountBefore, movementCountAfter);
    }

    [Fact]
    public async Task Adjustment_records_the_difference_from_a_physical_count()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        // The user counts 0 units. The ledger must store a change of -7.
        var movement = await service.RecordMovementAsync(
            productId: 1,
            locationId: 1,
            StockMovementType.Adjustment,
            quantity: 0,
            note: "Cycle count");

        var balance = await database.Context.InventoryBalances
            .SingleAsync(item => item.ProductId == 1 && item.LocationId == 1);

        Assert.Equal(0, balance.QuantityOnHand);
        Assert.Equal(7, movement.Quantity);
        Assert.Equal(-7, movement.QuantityChange);
        Assert.Equal(StockMovementType.Adjustment, movement.Type);
    }

    [Fact]
    public async Task Receive_creates_a_missing_product_location_balance()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        // Product 3 has no balance at OVERFLOW. The receive must create the row.
        await service.RecordMovementAsync(
            productId: 3,
            locationId: 2,
            StockMovementType.Receive,
            quantity: 12,
            note: null);

        var balance = await database.Context.InventoryBalances
            .SingleAsync(item => item.ProductId == 3 && item.LocationId == 2);

        Assert.Equal(12, balance.QuantityOnHand);
    }

    [Fact]
    public async Task Database_rejects_duplicate_skus_ignoring_case()
    {
        await using var database = await TestDatabase.CreateAsync();

        // ELEC-100 already exists. The unique index must reject elec-100.
        database.Context.Products.Add(new Product
        {
            Sku = "elec-100",
            Name = "Duplicate hub",
            UnitCost = 10,
            ReorderLevel = 1,
            CategoryId = 1
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Database_rejects_negative_unit_cost()
    {
        await using var database = await TestDatabase.CreateAsync();

        // The check constraint must reject a negative cost.
        database.Context.Products.Add(new Product
        {
            Sku = "TEST-NEGATIVE-COST",
            Name = "Invalid product",
            UnitCost = -1,
            ReorderLevel = 0,
            CategoryId = 1
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => database.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Dashboard_summarizes_test_inventory()
    {
        await using var database = await TestDatabase.CreateAsync();
        var controller = new HomeController(database.Context);

        var result = await controller.Index(CancellationToken.None);
        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<DashboardViewModel>(view.Model);

        // Seeded totals: 4 SKUs, 54 units, 3 low-stock items, 1 out of stock.
        Assert.Equal(4, model.TotalSkus);
        Assert.Equal(54, model.TotalUnits);
        Assert.Equal(3, model.LowStockCount);
        Assert.Equal(1, model.OutOfStockCount);
        Assert.Equal(3, model.LowStockItems.Count);
        Assert.Equal(4, model.RecentMovements.Count);
    }

    private static InventoryService CreateService(WarehouseDbContext context) =>
        new(context, new FixedTimeProvider(FixedNow));

    // Use a fixed time so test results do not depend on the system clock.
    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    /// <summary>
    /// Creates a private in-memory SQLite database for one test.
    /// </summary>
    private sealed class TestDatabase(
        SqliteConnection connection,
        WarehouseDbContext context) : IAsyncDisposable
    {
        public WarehouseDbContext Context { get; } = context;

        public static async Task<TestDatabase> CreateAsync()
        {
            // Keep this connection open to keep the in-memory database available.
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<WarehouseDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new WarehouseDbContext(options);

            await context.Database.EnsureCreatedAsync();
            await AddTestCatalogAsync(context);

            return new TestDatabase(connection, context);
        }

        /// <summary>
        /// Adds products and stock for tests. The application database does not store these rows.
        /// </summary>
        private static async Task AddTestCatalogAsync(WarehouseDbContext context)
        {
            var firstSeedDate = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);

            context.Products.AddRange(
                new Product
                {
                    Id = 1,
                    Sku = "ELEC-100",
                    Name = "USB-C hub",
                    Description = "Seven-port USB-C desk hub",
                    UnitCost = 29.99m,
                    ReorderLevel = 10,
                    CategoryId = 1
                },
                new Product
                {
                    Id = 2,
                    Sku = "OFF-210",
                    Name = "Shipping label roll",
                    Description = "Thermal 4 x 6 inch labels",
                    UnitCost = 8.50m,
                    ReorderLevel = 20,
                    CategoryId = 2
                },
                new Product
                {
                    Id = 3,
                    Sku = "SAFE-310",
                    Name = "Nitrile gloves",
                    Description = "Powder-free gloves, box of 100",
                    UnitCost = 12.75m,
                    ReorderLevel = 15,
                    CategoryId = 3
                },
                new Product
                {
                    Id = 4,
                    Sku = "ELEC-120",
                    Name = "Barcode scanner",
                    Description = "USB handheld barcode scanner",
                    UnitCost = 64.99m,
                    ReorderLevel = 3,
                    CategoryId = 1
                });

            context.InventoryBalances.AddRange(
                new InventoryBalance
                {
                    ProductId = 1,
                    LocationId = 1,
                    QuantityOnHand = 7,
                    UpdatedAtUtc = firstSeedDate
                },
                new InventoryBalance
                {
                    ProductId = 2,
                    LocationId = 1,
                    QuantityOnHand = 45,
                    UpdatedAtUtc = firstSeedDate.AddDays(2)
                },
                new InventoryBalance
                {
                    ProductId = 3,
                    LocationId = 1,
                    QuantityOnHand = 0,
                    UpdatedAtUtc = firstSeedDate
                },
                new InventoryBalance
                {
                    ProductId = 4,
                    LocationId = 2,
                    QuantityOnHand = 2,
                    UpdatedAtUtc = firstSeedDate.AddDays(1)
                });

            context.StockMovements.AddRange(
                new StockMovement
                {
                    Id = 1,
                    ProductId = 1,
                    LocationId = 1,
                    Type = StockMovementType.Receive,
                    Quantity = 7,
                    QuantityChange = 7,
                    Note = "Opening balance",
                    OccurredAtUtc = firstSeedDate
                },
                new StockMovement
                {
                    Id = 2,
                    ProductId = 2,
                    LocationId = 1,
                    Type = StockMovementType.Receive,
                    Quantity = 50,
                    QuantityChange = 50,
                    Note = "Opening delivery",
                    OccurredAtUtc = firstSeedDate.AddDays(1)
                },
                new StockMovement
                {
                    Id = 3,
                    ProductId = 2,
                    LocationId = 1,
                    Type = StockMovementType.Issue,
                    Quantity = 5,
                    QuantityChange = -5,
                    Note = "Packed customer orders",
                    OccurredAtUtc = firstSeedDate.AddDays(2)
                },
                new StockMovement
                {
                    Id = 4,
                    ProductId = 4,
                    LocationId = 2,
                    Type = StockMovementType.Receive,
                    Quantity = 2,
                    QuantityChange = 2,
                    Note = "Opening balance",
                    OccurredAtUtc = firstSeedDate.AddDays(1)
                });

            await context.SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
