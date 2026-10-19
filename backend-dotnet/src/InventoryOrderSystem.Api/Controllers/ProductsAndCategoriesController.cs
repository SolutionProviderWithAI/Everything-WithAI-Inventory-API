using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? query,
        [FromQuery] Guid? categoryId,
        [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var q = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .AsQueryable();

        if (activeOnly ?? true)
        {
            q = q.Where(p => p.IsActive);
        }

        if (categoryId.HasValue)
        {
            q = q.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var search = query.Trim().ToLower();
            q = q.Where(p => p.Name.ToLower().Contains(search) ||
                             p.Sku.ToLower().Contains(search) ||
                             (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        var total = await q.CountAsync(ct);
        var products = await q
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Sku = p.Sku,
                Name = p.Name,
                Description = p.Description,
                UnitOfMeasurement = p.UnitOfMeasurement,
                CostPrice = p.CostPrice,
                SellingPrice = p.SellingPrice,
                ReorderLevel = p.ReorderLevel,
                ReorderQuantity = p.ReorderQuantity,
                IsActive = p.IsActive,
                TotalQuantityOnHand = p.Inventories.Sum(i => i.QuantityOnHand),
                TotalReservedQuantity = p.Inventories.Sum(i => i.ReservedQuantity),
                AvailableToSell = p.Inventories.Sum(i => i.QuantityOnHand) - p.Inventories.Sum(i => i.ReservedQuantity),
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<ProductDto>(products, total, page, pageSize));
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(Guid id, CancellationToken ct)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        var dto = new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            UnitOfMeasurement = p.UnitOfMeasurement,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            ReorderLevel = p.ReorderLevel,
            ReorderQuantity = p.ReorderQuantity,
            IsActive = p.IsActive,
            TotalQuantityOnHand = p.Inventories.Sum(i => i.QuantityOnHand),
            TotalReservedQuantity = p.Inventories.Sum(i => i.ReservedQuantity),
            AvailableToSell = p.Inventories.Sum(i => i.QuantityOnHand) - p.Inventories.Sum(i => i.ReservedQuantity),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto, CancellationToken ct)
    {
        if (await _context.Products.AnyAsync(p => p.Sku.ToLower() == dto.Sku.Trim().ToLower(), ct))
        {
            throw new BusinessRuleValidationException($"Product with SKU '{dto.Sku}' already exists.");
        }

        var category = await _context.ProductCategories.FindAsync(new object[] { dto.CategoryId }, ct)
            ?? throw new NotFoundException(nameof(ProductCategory), dto.CategoryId);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = dto.CategoryId,
            Sku = dto.Sku.Trim().ToUpper(),
            Name = dto.Name.Trim(),
            Description = dto.Description,
            UnitOfMeasurement = string.IsNullOrWhiteSpace(dto.UnitOfMeasurement) ? "Unit" : dto.UnitOfMeasurement,
            CostPrice = dto.CostPrice,
            SellingPrice = dto.SellingPrice,
            ReorderLevel = dto.ReorderLevel,
            ReorderQuantity = dto.ReorderQuantity,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product, ct);

        // Default warehouse for initial stock
        var warehouseId = dto.InitialWarehouseId;
        if (!warehouseId.HasValue)
        {
            var primaryWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsPrimary, ct)
                ?? await _context.Warehouses.FirstOrDefaultAsync(ct);
            warehouseId = primaryWh?.Id;
        }

        if (warehouseId.HasValue)
        {
            var inv = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                WarehouseId = warehouseId.Value,
                QuantityOnHand = Math.Max(0, dto.InitialStock),
                ReservedQuantity = 0,
                DamagedQuantity = 0,
                MinimumStock = dto.ReorderLevel / 2,
                UpdatedAt = DateTime.UtcNow
            };
            await _context.Inventories.AddAsync(inv, ct);

            if (dto.InitialStock > 0)
            {
                var tx = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    WarehouseId = warehouseId.Value,
                    TransactionType = InventoryTransactionType.Receipt,
                    QuantityChange = dto.InitialStock,
                    BalanceOnHandAfter = dto.InitialStock,
                    BalanceReservedAfter = 0,
                    ReferenceType = "OpeningStock",
                    ReferenceId = "MANUAL-CREATE",
                    Notes = "Initial opening stock created with product",
                    CreatedBy = User.Identity?.Name ?? "Admin",
                    CreatedAt = DateTime.UtcNow
                };
                await _context.InventoryTransactions.AddAsync(tx, ct);
            }
        }

        await _context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, await GetProductDtoInternal(product.Id, ct));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.CategoryId = dto.CategoryId;
        product.Name = dto.Name.Trim();
        product.Description = dto.Description;
        product.UnitOfMeasurement = dto.UnitOfMeasurement;
        product.CostPrice = dto.CostPrice;
        product.SellingPrice = dto.SellingPrice;
        product.ReorderLevel = dto.ReorderLevel;
        product.ReorderQuantity = dto.ReorderQuantity;
        product.IsActive = dto.IsActive;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Ok(await GetProductDtoInternal(id, ct));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateProduct(Guid id, CancellationToken ct)
    {
        var product = await _context.Products.FindAsync(new object[] { id }, ct)
            ?? throw new NotFoundException(nameof(Product), id);

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<ProductDto> GetProductDtoInternal(Guid id, CancellationToken ct)
    {
        var p = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Inventories)
            .FirstAsync(p => p.Id == id, ct);

        return new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category.Name,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            UnitOfMeasurement = p.UnitOfMeasurement,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            ReorderLevel = p.ReorderLevel,
            ReorderQuantity = p.ReorderQuantity,
            IsActive = p.IsActive,
            TotalQuantityOnHand = p.Inventories.Sum(i => i.QuantityOnHand),
            TotalReservedQuantity = p.Inventories.Sum(i => i.ReservedQuantity),
            AvailableToSell = p.Inventories.Sum(i => i.QuantityOnHand) - p.Inventories.Sum(i => i.ReservedQuantity),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };
    }
}

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var categories = await _context.ProductCategories
            .Include(c => c.Products)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                ParentCategoryId = c.ParentCategoryId,
                IsActive = c.IsActive,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync(ct);

        return Ok(categories);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
    {
        if (await _context.ProductCategories.AnyAsync(c => c.Code.ToLower() == dto.Code.Trim().ToLower(), ct))
        {
            throw new BusinessRuleValidationException($"Category code '{dto.Code}' already exists.");
        }

        var cat = new ProductCategory
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Code = dto.Code.Trim().ToUpper(),
            Description = dto.Description,
            ParentCategoryId = dto.ParentCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.ProductCategories.AddAsync(cat, ct);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCategories), new { id = cat.Id }, new CategoryDto
        {
            Id = cat.Id,
            Name = cat.Name,
            Code = cat.Code,
            Description = cat.Description,
            ParentCategoryId = cat.ParentCategoryId,
            IsActive = cat.IsActive,
            ProductCount = 0
        });
    }
}
