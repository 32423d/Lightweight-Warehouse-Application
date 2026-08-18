namespace CS_Warehouse.Web.Models;

/// <summary>
/// Identifies how a stock movement changes quantity.
/// </summary>
public enum StockMovementType
{
    /// <summary>Adds stock to a location.</summary>
    Receive = 1,

    /// <summary>Removes stock from a location.</summary>
    Issue = 2,

    /// <summary>Sets the location quantity to a physical count.</summary>
    Adjustment = 3
}
