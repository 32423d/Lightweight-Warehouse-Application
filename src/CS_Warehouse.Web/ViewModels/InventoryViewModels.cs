using System.ComponentModel.DataAnnotations;
using CS_Warehouse.Web.Models;

namespace CS_Warehouse.Web.ViewModels;

/// <summary>
/// Collects the data for a new stock movement. Bind this type, not the entity.
/// </summary>
public sealed class StockMovementFormViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
    [Display(Name = "Product")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Select a location.")]
    [Display(Name = "Location")]
    public int LocationId { get; set; }

    [EnumDataType(typeof(StockMovementType))]
    [Display(Name = "Movement type")]
    public StockMovementType Type { get; set; } = StockMovementType.Receive;

    /// <summary>
    /// For receive and issue, this is the change size.
    /// For adjustment, this is the physical count.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }

    public IReadOnlyList<LookupOptionViewModel> Products { get; set; } = [];
    public IReadOnlyList<LookupOptionViewModel> Locations { get; set; } = [];
}

/// <summary>
/// Holds filter values and movement rows for the history page.
/// </summary>
public sealed class MovementHistoryViewModel
{
    public string? Search { get; init; }
    public int? ProductId { get; init; }
    public int? LocationId { get; init; }
    public StockMovementType? Type { get; init; }
    public IReadOnlyList<LookupOptionViewModel> Products { get; init; } = [];
    public IReadOnlyList<LookupOptionViewModel> Locations { get; init; } = [];
    public IReadOnlyList<StockMovementListItemViewModel> Movements { get; init; } = [];
}

/// <summary>
/// Shows one movement on list pages. Quantity is the display size.
/// QuantityChange is the signed effect on stock.
/// </summary>
public sealed class StockMovementListItemViewModel
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductSku { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string LocationCode { get; init; } = string.Empty;
    public StockMovementType Type { get; init; }
    public int Quantity { get; init; }
    public int QuantityChange { get; init; }
    public string? Note { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}
