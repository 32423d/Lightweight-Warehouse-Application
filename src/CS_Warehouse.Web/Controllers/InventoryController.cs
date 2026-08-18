using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Models;
using CS_Warehouse.Web.Services;
using CS_Warehouse.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Web.Controllers;

/// <summary>
/// Shows the stock movement history and records new movements.
/// </summary>
public sealed class InventoryController(
    WarehouseDbContext dbContext,
    IInventoryService inventoryService) : Controller
{
    /// <summary>
    /// Lists recent movements. Users can filter by product, location, type, or search text.
    /// </summary>
    public async Task<IActionResult> Index(
        string? search,
        int? productId,
        int? locationId,
        StockMovementType? type,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.StockMovements.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(movement =>
                movement.Product.Sku.Contains(term)
                || movement.Product.Name.Contains(term)
                || (movement.Note != null && movement.Note.Contains(term)));
        }

        if (productId.HasValue)
        {
            query = query.Where(movement => movement.ProductId == productId.Value);
        }

        if (locationId.HasValue)
        {
            query = query.Where(movement => movement.LocationId == locationId.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(movement => movement.Type == type.Value);
        }

        // Limit the page size so a large ledger stays usable.
        var movements = await query
            .OrderByDescending(movement => movement.OccurredAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Take(200)
            .Select(movement => new StockMovementListItemViewModel
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductSku = movement.Product.Sku,
                ProductName = movement.Product.Name,
                LocationCode = movement.Location.Code,
                Type = movement.Type,
                Quantity = movement.Quantity,
                QuantityChange = movement.QuantityChange,
                Note = movement.Note,
                OccurredAtUtc = movement.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        return View(new MovementHistoryViewModel
        {
            Search = search?.Trim(),
            ProductId = productId,
            LocationId = locationId,
            Type = type,
            Products = await GetProductsAsync(includeArchived: true, cancellationToken),
            Locations = await GetLocationsAsync(cancellationToken),
            Movements = movements
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        int? productId,
        CancellationToken cancellationToken)
    {
        var products = await GetProductsAsync(
            includeArchived: false,
            cancellationToken);
        var locations = await GetLocationsAsync(cancellationToken);

        return View(new StockMovementFormViewModel
        {
            // Preselect the product when the user opens this page from product details.
            ProductId = productId.HasValue
                && products.Any(product => product.Id == productId.Value)
                    ? productId.Value
                    : 0,
            LocationId = locations.FirstOrDefault()?.Id ?? 0,
            Products = products,
            Locations = locations
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        StockMovementFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // The service owns the stock rules. This controller only handles HTTP.
                var movement = await inventoryService.RecordMovementAsync(
                    model.ProductId,
                    model.LocationId,
                    model.Type,
                    model.Quantity,
                    model.Note,
                    cancellationToken);

                TempData["SuccessMessage"] =
                    $"{movement.Type} movement recorded ({movement.QuantityChange:+#;-#;0} units).";
                return RedirectToAction(nameof(Index));
            }
            catch (InventoryOperationException exception)
            {
                // Show the service message on the form. Do not change the page.
                ModelState.AddModelError(string.Empty, exception.Message);
            }
        }

        model.Products = await GetProductsAsync(
            includeArchived: false,
            cancellationToken);
        model.Locations = await GetLocationsAsync(cancellationToken);
        return View(model);
    }

    private async Task<IReadOnlyList<LookupOptionViewModel>> GetProductsAsync(
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Products.AsNoTracking();
        if (!includeArchived)
        {
            // New movements can use active products only.
            query = query.Where(product => !product.IsArchived);
        }

        return await query
            .OrderBy(product => product.Sku)
            .Select(product => new LookupOptionViewModel(
                product.Id,
                product.Sku + " — " + product.Name))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LookupOptionViewModel>> GetLocationsAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Locations
            .AsNoTracking()
            .OrderBy(location => location.Code)
            .Select(location => new LookupOptionViewModel(
                location.Id,
                location.Code + " — " + location.Name))
            .ToListAsync(cancellationToken);
    }
}
