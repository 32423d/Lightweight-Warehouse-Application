using System.ComponentModel.DataAnnotations;

namespace CS_Warehouse.Web.Models;

/// <summary>
/// Stores a catalog item. Stock is not stored here.
/// Location balances hold the current quantities.
/// </summary>
public sealed class Product
{
    public int Id { get; set; }

    [Required, StringLength(32)]
    [Display(Name = "SKU")]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 1_000_000)]
    [Display(Name = "Unit cost")]
    public decimal UnitCost { get; set; }

    /// <summary>
    /// The quantity that starts a low-stock warning.
    /// Compare this value with the total of all location balances.
    /// </summary>
    [Range(0, int.MaxValue)]
    [Display(Name = "Reorder level")]
    public int ReorderLevel { get; set; }

    [Display(Name = "Category")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Hidden products stay in history. Users cannot record new stock for them.
    /// </summary>
    public bool IsArchived { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<InventoryBalance> InventoryBalances { get; set; } = [];

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
