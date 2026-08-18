using System.ComponentModel.DataAnnotations;

namespace CS_Warehouse.Web.ViewModels;

/// <summary>
/// Holds search filters and the product list for the catalog page.
/// </summary>
public sealed class ProductIndexViewModel
{
    public string? Search { get; init; }
    public int? CategoryId { get; init; }
    public bool IncludeArchived { get; init; }
    public IReadOnlyList<LookupOptionViewModel> Categories { get; init; } = [];
    public IReadOnlyList<ProductStockListItemViewModel> Products { get; init; } = [];
}

/// <summary>
/// Collects product form fields. This type prevents over-posting of entity properties.
/// </summary>
public sealed class ProductFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    [RegularExpression(
        @"^[A-Za-z0-9][A-Za-z0-9._-]*$",
        ErrorMessage = "Use letters, numbers, periods, hyphens, or underscores.")]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(typeof(decimal), "0", "1000000")]
    [Display(Name = "Unit cost")]
    public decimal UnitCost { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a category.")]
    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    [Display(Name = "Archived")]
    public bool IsArchived { get; set; }

    public IReadOnlyList<LookupOptionViewModel> Categories { get; set; } = [];
}

/// <summary>
/// Shows one product, the quantity at each location, and recent movements.
/// </summary>
public sealed class ProductDetailsViewModel
{
    public int Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal UnitCost { get; init; }
    public int ReorderLevel { get; init; }
    public int TotalQuantity { get; init; }
    public bool IsArchived { get; init; }
    public IReadOnlyList<LocationBalanceViewModel> Balances { get; init; } = [];
    public IReadOnlyList<StockMovementListItemViewModel> RecentMovements { get; init; } = [];
}

/// <summary>
/// Shows the current quantity for one product at one location.
/// </summary>
public sealed class LocationBalanceViewModel
{
    public int LocationId { get; init; }
    public string LocationCode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public int QuantityOnHand { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
