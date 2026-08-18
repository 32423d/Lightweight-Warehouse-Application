using System.ComponentModel.DataAnnotations;

namespace CS_Warehouse.Web.Models;

/// <summary>
/// Identifies a storage place. One product can have a balance at each location.
/// </summary>
public sealed class Location
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    public ICollection<InventoryBalance> InventoryBalances { get; set; } = [];

    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
