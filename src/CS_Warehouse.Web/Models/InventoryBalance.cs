namespace CS_Warehouse.Web.Models;

/// <summary>
/// Stores the current quantity for one product at one location.
/// The movement ledger explains how this quantity changed.
/// </summary>
public sealed class InventoryBalance
{
    public int ProductId { get; set; }

    public int LocationId { get; set; }

    /// <summary>
    /// The current quantity at this location. This value cannot be negative.
    /// </summary>
    public int QuantityOnHand { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public Product Product { get; set; } = null!;

    public Location Location { get; set; } = null!;
}
