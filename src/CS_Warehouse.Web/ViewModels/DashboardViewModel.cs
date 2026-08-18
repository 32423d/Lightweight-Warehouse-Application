namespace CS_Warehouse.Web.ViewModels;

/// <summary>
/// Holds dashboard totals, low-stock products, and recent movements.
/// </summary>
public sealed class DashboardViewModel
{
    public int TotalSkus { get; init; }
    public int TotalUnits { get; init; }
    public int LowStockCount { get; init; }
    public int OutOfStockCount { get; init; }
    public IReadOnlyList<ProductStockListItemViewModel> LowStockItems { get; init; } = [];
    public IReadOnlyList<StockMovementListItemViewModel> RecentMovements { get; init; } = [];
}

/// <summary>
/// Shows one product row with its total quantity across all locations.
/// </summary>
public sealed class ProductStockListItemViewModel
{
    public int Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public decimal UnitCost { get; init; }
    public int ReorderLevel { get; init; }
    public int QuantityOnHand { get; init; }
    public bool IsArchived { get; init; }

    public bool IsOutOfStock => QuantityOnHand == 0;

    /// <summary>
    /// A product is low stock when the total quantity is at or below the reorder level.
    /// </summary>
    public bool IsLowStock => QuantityOnHand <= ReorderLevel;
}
