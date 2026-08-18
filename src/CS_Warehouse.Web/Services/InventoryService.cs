using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Web.Services;

/// <summary>
/// Applies stock-change rules in one database transaction.
/// The service updates the current balance and the movement ledger together.
/// </summary>
public sealed class InventoryService(
    WarehouseDbContext dbContext,
    TimeProvider timeProvider) : IInventoryService
{
    /// <summary>
    /// Records one stock movement and updates the related location balance.
    /// The method does not change the database when a rule fails.
    /// </summary>
    public async Task<StockMovement> RecordMovementAsync(
        int productId,
        int locationId,
        StockMovementType type,
        int quantity,
        string? note,
        CancellationToken cancellationToken = default)
    {
        // Reject invalid input before the transaction starts.
        ValidateRequest(type, quantity, note);

        // Keep the balance and movement record in one database transaction.
        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Do not change stock for an archived product.
        var productExists = await dbContext.Products
            .AnyAsync(
                product => product.Id == productId && !product.IsArchived,
                cancellationToken);
        if (!productExists)
        {
            throw new InventoryOperationException("Select an active product.");
        }

        var locationExists = await dbContext.Locations
            .AnyAsync(location => location.Id == locationId, cancellationToken);
        if (!locationExists)
        {
            throw new InventoryOperationException("Select a valid location.");
        }

        // A missing row means this product has no stock at this location yet.
        var balance = await dbContext.InventoryBalances
            .SingleOrDefaultAsync(
                item => item.ProductId == productId && item.LocationId == locationId,
                cancellationToken);

        var currentQuantity = balance?.QuantityOnHand ?? 0;
        var quantityChange = CalculateQuantityChange(type, quantity, currentQuantity);
        int updatedQuantity;

        try
        {
            // Stop the operation if the quantity exceeds the supported integer range.
            updatedQuantity = checked(currentQuantity + quantityChange);
        }
        catch (OverflowException exception)
        {
            throw new InventoryOperationException(
                "The resulting stock quantity is too large.",
                exception);
        }

        // Do not permit a negative location balance.
        if (updatedQuantity < 0)
        {
            throw new InventoryOperationException(
                $"Only {currentQuantity} unit(s) are available at this location.");
        }

        // Use the injected clock so tests can supply a fixed time.
        var occurredAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        if (balance is null)
        {
            // Create the first balance for this product and location pair.
            balance = new InventoryBalance
            {
                ProductId = productId,
                LocationId = locationId,
                QuantityOnHand = updatedQuantity,
                UpdatedAtUtc = occurredAtUtc
            };
            dbContext.InventoryBalances.Add(balance);
        }
        else
        {
            balance.QuantityOnHand = updatedQuantity;
            balance.UpdatedAtUtc = occurredAtUtc;
        }

        // Store a positive quantity for display and a signed change for calculations.
        var movement = new StockMovement
        {
            ProductId = productId,
            LocationId = locationId,
            Type = type,
            Quantity = Math.Abs(quantityChange),
            QuantityChange = quantityChange,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            OccurredAtUtc = occurredAtUtc
        };
        dbContext.StockMovements.Add(movement);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            // The transaction rolls back when this method exits without a commit.
            throw new InventoryOperationException(
                "The stock update could not be saved.",
                exception);
        }

        return movement;
    }

    /// <summary>
    /// Converts the user quantity into a signed stock change.
    /// </summary>
    private static int CalculateQuantityChange(
        StockMovementType type,
        int quantity,
        int currentQuantity)
    {
        // For an adjustment, quantity is the physical count. Store only the difference.
        return type switch
        {
            StockMovementType.Receive => quantity,
            StockMovementType.Issue => -quantity,
            StockMovementType.Adjustment when quantity == currentQuantity =>
                throw new InventoryOperationException(
                    "The counted quantity already matches the current stock."),
            StockMovementType.Adjustment => quantity - currentQuantity,
            _ => throw new InventoryOperationException("Select a valid movement type.")
        };
    }

    /// <summary>
    /// Checks movement type, quantity, and note length before any database write.
    /// </summary>
    private static void ValidateRequest(
        StockMovementType type,
        int quantity,
        string? note)
    {
        if (!Enum.IsDefined(type))
        {
            throw new InventoryOperationException("Select a valid movement type.");
        }

        if (type == StockMovementType.Adjustment)
        {
            // A physical count can be zero. It cannot be negative.
            if (quantity < 0)
            {
                throw new InventoryOperationException(
                    "A counted quantity cannot be negative.");
            }
        }
        else if (quantity <= 0)
        {
            // Receipts and issues must change stock.
            throw new InventoryOperationException("Quantity must be greater than zero.");
        }

        if (note?.Trim().Length > 250)
        {
            throw new InventoryOperationException(
                "The note cannot be longer than 250 characters.");
        }
    }
}
