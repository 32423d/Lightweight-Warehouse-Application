using System.ComponentModel.DataAnnotations;

namespace CS_Warehouse.Web.Models;

/// <summary>
/// Records one stock change. Users cannot edit or delete this record after save.
/// </summary>
public sealed class StockMovement
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int LocationId { get; set; }

    public StockMovementType Type { get; set; }

    /// <summary>
    /// The size of the change as a positive number. Use this value for display.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// The signed change applied to the location balance.
    /// A receipt is positive. An issue is negative.
    /// </summary>
    public int QuantityChange { get; set; }

    [StringLength(250)]
    public string? Note { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public Product Product { get; set; } = null!;

    public Location Location { get; set; } = null!;
}
