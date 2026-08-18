using CS_Warehouse.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Web.Data;

/// <summary>
/// Maps warehouse entities to SQLite and applies database constraints.
/// </summary>
public sealed class WarehouseDbContext(DbContextOptions<WarehouseDbContext> options)
    : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Make category names unique regardless of letter case.
        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(category => category.Name).UseCollation("NOCASE");
            entity.HasIndex(category => category.Name).IsUnique();
        });

        // Make each SKU unique regardless of letter case.
        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(product => product.Sku).UseCollation("NOCASE");
            entity.Property(product => product.UnitCost).HasPrecision(10, 2);
            entity.HasIndex(product => product.Sku).IsUnique();

            // Speed list queries that filter archived products by name.
            entity.HasIndex(product => new { product.IsArchived, product.Name });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_Product_UnitCost_NonNegative",
                    "CAST(UnitCost AS REAL) >= 0");
                table.HasCheckConstraint("CK_Product_ReorderLevel_NonNegative", "ReorderLevel >= 0");
            });

            // Do not delete a category that still has products.
            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Make each location code unique regardless of letter case.
        modelBuilder.Entity<Location>(entity =>
        {
            entity.Property(location => location.Code).UseCollation("NOCASE");
            entity.HasIndex(location => location.Code).IsUnique();
        });

        // Keep balances valid when a process bypasses the inventory service.
        modelBuilder.Entity<InventoryBalance>(entity =>
        {
            // Permit only one balance for each product and location pair.
            entity.HasKey(balance => new { balance.ProductId, balance.LocationId });
            entity.HasIndex(balance => balance.LocationId);
            entity.ToTable(table =>
                table.HasCheckConstraint(
                    "CK_InventoryBalance_QuantityOnHand_NonNegative",
                    "QuantityOnHand >= 0"));

            // Keep history when a user archives a product.
            entity.HasOne(balance => balance.Product)
                .WithMany(product => product.InventoryBalances)
                .HasForeignKey(balance => balance.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(balance => balance.Location)
                .WithMany(location => location.InventoryBalances)
                .HasForeignKey(balance => balance.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Keep each movement valid when a process writes directly to the database.
        modelBuilder.Entity<StockMovement>(entity =>
        {
            // Store the movement type as text so the database stays readable.
            entity.Property(movement => movement.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Support history filters by product or location and time.
            entity.HasIndex(movement => new { movement.ProductId, movement.OccurredAtUtc });
            entity.HasIndex(movement => new { movement.LocationId, movement.OccurredAtUtc });
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("CK_StockMovement_Quantity_Positive", "Quantity > 0");
                table.HasCheckConstraint("CK_StockMovement_QuantityChange_NonZero", "QuantityChange <> 0");
            });
            entity.HasOne(movement => movement.Product)
                .WithMany(product => product.StockMovements)
                .HasForeignKey(movement => movement.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(movement => movement.Location)
                .WithMany(location => location.StockMovements)
                .HasForeignKey(movement => movement.LocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        SeedReferenceData(modelBuilder);
    }

    /// <summary>
    /// Loads categories and locations. Users add products and stock after startup.
    /// </summary>
    private static void SeedReferenceData(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Electronics" },
            new Category { Id = 2, Name = "Office supplies" },
            new Category { Id = 3, Name = "Safety" });

        modelBuilder.Entity<Location>().HasData(
            new Location { Id = 1, Code = "MAIN", Name = "Main floor" },
            new Location { Id = 2, Code = "OVERFLOW", Name = "Overflow storage" });
    }
}
