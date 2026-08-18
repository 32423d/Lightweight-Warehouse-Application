using CS_Warehouse.Web.Data;
using CS_Warehouse.Web.Models;
using CS_Warehouse.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CS_Warehouse.Web.Controllers;

/// <summary>
/// Lists, creates, and updates catalog products.
/// This controller does not change stock quantities.
/// </summary>
public sealed class ProductsController(WarehouseDbContext dbContext) : Controller
{
    /// <summary>
    /// Lists products. Users can search, filter by category, and include archived items.
    /// </summary>
    public async Task<IActionResult> Index(
        string? search,
        int? categoryId,
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Products.AsNoTracking().AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(product => !product.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(product =>
                product.Sku.Contains(term) || product.Name.Contains(term));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(product => product.CategoryId == categoryId.Value);
        }

        var products = await query
            .OrderBy(product => product.Name)
            .Select(product => new ProductStockListItemViewModel
            {
                Id = product.Id,
                Sku = product.Sku,
                Name = product.Name,
                CategoryName = product.Category.Name,
                UnitCost = product.UnitCost,
                ReorderLevel = product.ReorderLevel,
                QuantityOnHand = product.InventoryBalances
                    .Sum(balance => (int?)balance.QuantityOnHand) ?? 0,
                IsArchived = product.IsArchived
            })
            .ToListAsync(cancellationToken);

        return View(new ProductIndexViewModel
        {
            Search = search?.Trim(),
            CategoryId = categoryId,
            IncludeArchived = includeArchived,
            Categories = await GetCategoriesAsync(cancellationToken),
            Products = products
        });
    }

    /// <summary>
    /// Shows one product, its location balances, and its recent movements.
    /// </summary>
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new
            {
                item.Id,
                item.Sku,
                item.Name,
                item.Description,
                CategoryName = item.Category.Name,
                item.UnitCost,
                item.ReorderLevel,
                item.IsArchived
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        var balances = await dbContext.InventoryBalances
            .AsNoTracking()
            .Where(balance => balance.ProductId == id)
            .OrderBy(balance => balance.Location.Code)
            .Select(balance => new LocationBalanceViewModel
            {
                LocationId = balance.LocationId,
                LocationCode = balance.Location.Code,
                LocationName = balance.Location.Name,
                QuantityOnHand = balance.QuantityOnHand,
                UpdatedAtUtc = balance.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var movements = await dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.ProductId == id)
            .OrderByDescending(movement => movement.OccurredAtUtc)
            .ThenByDescending(movement => movement.Id)
            .Take(20)
            .Select(movement => new StockMovementListItemViewModel
            {
                Id = movement.Id,
                ProductId = movement.ProductId,
                ProductSku = product.Sku,
                ProductName = product.Name,
                LocationCode = movement.Location.Code,
                Type = movement.Type,
                Quantity = movement.Quantity,
                QuantityChange = movement.QuantityChange,
                Note = movement.Note,
                OccurredAtUtc = movement.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        return View(new ProductDetailsViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            CategoryName = product.CategoryName,
            UnitCost = product.UnitCost,
            ReorderLevel = product.ReorderLevel,
            TotalQuantity = balances.Sum(balance => balance.QuantityOnHand),
            IsArchived = product.IsArchived,
            Balances = balances,
            RecentMovements = movements
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        return View(new ProductFormViewModel
        {
            Categories = await GetCategoriesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        Normalize(model);
        await ValidateProductAsync(model, productId: null, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        var product = new Product
        {
            Sku = model.Sku,
            Name = model.Name,
            Description = model.Description,
            UnitCost = model.UnitCost,
            ReorderLevel = model.ReorderLevel,
            CategoryId = model.CategoryId
        };

        dbContext.Products.Add(product);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Catch a duplicate SKU that another request saved after the first check.
            ModelState.AddModelError(
                nameof(model.Sku),
                "That SKU is already in use.");
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"{product.Sku} was created.";
        return RedirectToAction(nameof(Details), new { id = product.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var product = await dbContext.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return View(new ProductFormViewModel
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            UnitCost = product.UnitCost,
            ReorderLevel = product.ReorderLevel,
            CategoryId = product.CategoryId,
            IsArchived = product.IsArchived,
            Categories = await GetCategoriesAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        ProductFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        Normalize(model);
        await ValidateProductAsync(model, id, cancellationToken);

        if (!ModelState.IsValid)
        {
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        var product = await dbContext.Products
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        // Copy only form fields. Do not bind the full entity from the request.
        product.Sku = model.Sku;
        product.Name = model.Name;
        product.Description = model.Description;
        product.UnitCost = model.UnitCost;
        product.ReorderLevel = model.ReorderLevel;
        product.CategoryId = model.CategoryId;
        product.IsArchived = model.IsArchived;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(
                nameof(model.Sku),
                "That SKU is already in use.");
            model.Categories = await GetCategoriesAsync(cancellationToken);
            return View(model);
        }

        TempData["SuccessMessage"] = $"{product.Sku} was updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>
    /// Checks that the category exists and that the SKU is unique.
    /// </summary>
    private async Task ValidateProductAsync(
        ProductFormViewModel model,
        int? productId,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Categories
                .AnyAsync(category => category.Id == model.CategoryId, cancellationToken))
        {
            ModelState.AddModelError(
                nameof(model.CategoryId),
                "Select a valid category.");
        }

        if (await dbContext.Products.AnyAsync(
                product =>
                    product.Sku == model.Sku
                    && (!productId.HasValue || product.Id != productId.Value),
                cancellationToken))
        {
            ModelState.AddModelError(nameof(model.Sku), "That SKU is already in use.");
        }
    }

    private async Task<IReadOnlyList<LookupOptionViewModel>> GetCategoriesAsync(
        CancellationToken cancellationToken)
    {
        return await dbContext.Categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .Select(category => new LookupOptionViewModel(category.Id, category.Name))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Trims text fields and stores the SKU in uppercase.
    /// </summary>
    private static void Normalize(ProductFormViewModel model)
    {
        model.Sku = model.Sku?.Trim().ToUpperInvariant() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
    }
}
