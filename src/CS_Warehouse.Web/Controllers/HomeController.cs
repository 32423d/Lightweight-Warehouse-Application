using System.Diagnostics;
using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Models;
using CS_Warehouse.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Web.Controllers;

/// <summary>
/// Shows dashboard totals and recent stock activity.
/// </summary>
public sealed class HomeController(WarehouseDbContext dbContext) : Controller
{
    /// <summary>
    /// Builds the dashboard from current location balances and recent movements.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        // Do not track these rows. The dashboard does not change them.
        var products = await dbContext.Products
            .AsNoTracking()
            .Where(product => !product.IsArchived)
            .Select(product => new ProductStockListItemViewModel
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                CategoryName = product.Category.Name,
                UnitCost = product.UnitCost,
                ReorderLevel = product.ReorderLevel,
                // Add the quantity from every location for this product.
                QuantityOnHand = product.InventoryBalances
                    .Sum(balance => (int?)balance.QuantityOnHand) ?? 0
            })
            .ToListAsync(cancellationToken);

        var recentMovements = await dbContext.StockMovements
            .AsNoTracking()
            .OrderByDescending(movement => movement.OccurredAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Take(5)
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

        // A product is low stock when its total quantity is at or below the reorder level.
        var lowStockItems = products
            .Where(product => product.IsLowStock)
            .OrderBy(product => product.QuantityOnHand)
            .ThenBy(product => product.Name)
            .ToList();

        return View(new DashboardViewModel
        {
            TotalSkus = products.Count,
            TotalUnits = products.Sum(product => product.QuantityOnHand),
            LowStockCount = lowStockItems.Count,
            OutOfStockCount = products.Count(product => product.IsOutOfStock),
            LowStockItems = lowStockItems,
            RecentMovements = recentMovements
        });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
