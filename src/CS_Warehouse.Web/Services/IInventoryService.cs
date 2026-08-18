using CS_Warehouse.Web.Models;

namespace CS_Warehouse.Web.Services;

/// <summary>
/// Records stock receipts, issues, and physical-count adjustments.
/// </summary>
public interface IInventoryService
{
    /// <summary>
    /// Updates the location balance and writes one audit record.
    /// </summary>
    /// <param name="productId">The product that changes.</param>
    /// <param name="locationId">The storage location that changes.</param>
    /// <param name="type">The movement type: receive, issue, or adjustment.</param>
    /// <param name="quantity">
    /// The received or issued quantity. For an adjustment, this value is the physical count.
    /// </param>
    /// <param name="note">An optional note. The service stores a trimmed value.</param>
    /// <param name="cancellationToken">Stops the operation when the request ends.</param>
    Task<StockMovement> RecordMovementAsync(
        int productId,
        int locationId,
        StockMovementType type,
        int quantity,
        string? note,
        CancellationToken cancellationToken = default);
}
