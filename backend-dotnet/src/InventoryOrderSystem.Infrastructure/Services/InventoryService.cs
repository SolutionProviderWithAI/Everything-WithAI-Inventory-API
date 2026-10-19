using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _context;

    public InventoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetAvailableToSellAsync(Guid productId, Guid? warehouseId = null, CancellationToken ct = default)
    {
        var query = _context.Inventories.Where(i => i.ProductId == productId);
        if (warehouseId.HasValue)
        {
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        }

        var stockList = await query.ToListAsync(ct);
        if (!stockList.Any())
        {
            return 0;
        }

        return stockList.Sum(i => i.AvailableToSell);
    }

    public async Task<List<InventoryDto>> GetProductStockAsync(Guid productId, CancellationToken ct = default)
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.ProductId == productId)
            .Select(i => new InventoryDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductSku = i.Product.Sku,
                ProductName = i.Product.Name,
                WarehouseId = i.WarehouseId,
                WarehouseCode = i.Warehouse.Code,
                WarehouseName = i.Warehouse.Name,
                QuantityOnHand = i.QuantityOnHand,
                ReservedQuantity = i.ReservedQuantity,
                DamagedQuantity = i.DamagedQuantity,
                MinimumStock = i.MinimumStock,
                UpdatedAt = i.UpdatedAt
            })
            .ToListAsync(ct);
    }

    public async Task<List<LowStockDto>> GetLowStockAlertsAsync(CancellationToken ct = default)
    {
        var products = await _context.Products
            .Include(p => p.Inventories)
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        var list = new List<LowStockDto>();
        foreach (var p in products)
        {
            var onHand = p.Inventories.Sum(i => i.QuantityOnHand);
            var reserved = p.Inventories.Sum(i => i.ReservedQuantity);
            var available = onHand - reserved;

            if (available <= p.ReorderLevel)
            {
                list.Add(new LowStockDto
                {
                    ProductId = p.Id,
                    Sku = p.Sku,
                    Name = p.Name,
                    QuantityOnHand = onHand,
                    ReservedQuantity = reserved,
                    AvailableToSell = available,
                    ReorderLevel = p.ReorderLevel,
                    ReorderQuantity = p.ReorderQuantity,
                    Status = available <= 0 ? "OutOfStock" : "LowStock"
                });
            }
        }

        return list;
    }

    public async Task<PagedResult<InventoryMovementDto>> GetStockMovementsAsync(
        Guid? productId, Guid? warehouseId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.Warehouse)
            .AsQueryable();

        if (productId.HasValue)
        {
            query = query.Where(t => t.ProductId == productId.Value);
        }
        if (warehouseId.HasValue)
        {
            query = query.Where(t => t.WarehouseId == warehouseId.Value);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new InventoryMovementDto
            {
                Id = t.Id,
                ProductId = t.ProductId,
                ProductSku = t.Product.Sku,
                ProductName = t.Product.Name,
                WarehouseId = t.WarehouseId,
                WarehouseCode = t.Warehouse.Code,
                TransactionType = t.TransactionType.ToString(),
                QuantityChange = t.QuantityChange,
                BalanceOnHandAfter = t.BalanceOnHandAfter,
                BalanceReservedAfter = t.BalanceReservedAfter,
                ReferenceType = t.ReferenceType,
                ReferenceId = t.ReferenceId,
                Notes = t.Notes,
                CreatedBy = t.CreatedBy,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(ct);

        return new PagedResult<InventoryMovementDto>(items, total, page, pageSize);
    }

    public async Task<bool> ReserveStockAsync(Guid salesOrderId, Guid productId, int quantity, Guid? warehouseId = null, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            throw new BusinessRuleValidationException("Reservation quantity must be strictly greater than zero.");
        }

        // Query product
        var product = await _context.Products.FindAsync(new object[] { productId }, ct)
            ?? throw new NotFoundException(nameof(Product), productId);

        // Find primary or specified warehouse inventory
        var invQuery = _context.Inventories
            .Include(i => i.Warehouse)
            .Where(i => i.ProductId == productId);

        if (warehouseId.HasValue)
        {
            invQuery = invQuery.Where(i => i.WarehouseId == warehouseId.Value);
        }
        else
        {
            // Prefer primary warehouse, or first with available stock
            invQuery = invQuery.OrderByDescending(i => i.Warehouse.IsPrimary)
                               .ThenByDescending(i => i.QuantityOnHand - i.ReservedQuantity);
        }

        var inv = await invQuery.FirstOrDefaultAsync(ct);
        if (inv == null)
        {
            // Auto create warehouse inventory if warehouse exists
            var defaultWh = await _context.Warehouses.FirstOrDefaultAsync(w => w.IsPrimary, ct)
                ?? await _context.Warehouses.FirstOrDefaultAsync(ct)
                ?? throw new BusinessRuleValidationException("No warehouse exists to hold inventory.");

            inv = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                WarehouseId = defaultWh.Id,
                QuantityOnHand = 0,
                ReservedQuantity = 0,
                DamagedQuantity = 0,
                MinimumStock = product.ReorderLevel / 2
            };
            await _context.Inventories.AddAsync(inv, ct);
            await _context.SaveChangesAsync(ct);
        }

        var available = inv.AvailableToSell;
        if (available < quantity)
        {
            throw new InsufficientStockException(productId, product.Sku, quantity, available);
        }

        // Atomic reservation increment
        inv.ReservedQuantity += quantity;
        inv.UpdatedAt = DateTime.UtcNow;
        inv.ConcurrencyToken = Guid.NewGuid();

        // Create reservation entry
        var reservation = new InventoryReservation
        {
            Id = Guid.NewGuid(),
            SalesOrderId = salesOrderId,
            ProductId = productId,
            WarehouseId = inv.WarehouseId,
            Quantity = quantity,
            Status = ReservationStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddDays(3),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.InventoryReservations.AddAsync(reservation, ct);

        // Record transaction
        var tx = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            WarehouseId = inv.WarehouseId,
            TransactionType = InventoryTransactionType.Reservation,
            QuantityChange = quantity,
            BalanceOnHandAfter = inv.QuantityOnHand,
            BalanceReservedAfter = inv.ReservedQuantity,
            ReferenceType = "SalesOrder",
            ReferenceId = salesOrderId.ToString(),
            Notes = $"Stock reserved for Order {salesOrderId}",
            CreatedBy = "System",
            CreatedAt = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(tx, ct);

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task ReleaseReservationAsync(Guid salesOrderId, string reason = "Order cancelled", string userId = "System", CancellationToken ct = default)
    {
        var reservations = await _context.InventoryReservations
            .Where(r => r.SalesOrderId == salesOrderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var res in reservations)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == res.ProductId && i.WarehouseId == res.WarehouseId, ct);

            if (inv != null)
            {
                inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - res.Quantity);
                inv.UpdatedAt = DateTime.UtcNow;
                inv.ConcurrencyToken = Guid.NewGuid();

                var tx = new InventoryTransaction
                {
                    Id = Guid.NewGuid(),
                    ProductId = res.ProductId,
                    WarehouseId = res.WarehouseId,
                    TransactionType = InventoryTransactionType.ReservationRelease,
                    QuantityChange = -res.Quantity,
                    BalanceOnHandAfter = inv.QuantityOnHand,
                    BalanceReservedAfter = inv.ReservedQuantity,
                    ReferenceType = "SalesOrder",
                    ReferenceId = salesOrderId.ToString(),
                    Notes = $"Reservation released: {reason}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _context.InventoryTransactions.AddAsync(tx, ct);
            }

            res.Status = ReservationStatus.Cancelled;
            res.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task FulfillReservationAndDeductStockAsync(Guid salesOrderId, string userId = "System", CancellationToken ct = default)
    {
        var reservations = await _context.InventoryReservations
            .Where(r => r.SalesOrderId == salesOrderId && r.Status == ReservationStatus.Active)
            .ToListAsync(ct);

        foreach (var res in reservations)
        {
            var inv = await _context.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == res.ProductId && i.WarehouseId == res.WarehouseId, ct)
                ?? throw new BusinessRuleValidationException($"Inventory record not found for reserved product {res.ProductId}");

            // Deduct from physical on-hand and release reservation
            if (inv.QuantityOnHand < res.Quantity)
            {
                throw new InsufficientStockException(res.ProductId, "N/A", res.Quantity, inv.QuantityOnHand);
            }

            inv.QuantityOnHand -= res.Quantity;
            inv.ReservedQuantity = Math.Max(0, inv.ReservedQuantity - res.Quantity);
            inv.UpdatedAt = DateTime.UtcNow;
            inv.ConcurrencyToken = Guid.NewGuid();

            res.Status = ReservationStatus.Fulfilled;
            res.UpdatedAt = DateTime.UtcNow;

            var tx = new InventoryTransaction
            {
                Id = Guid.NewGuid(),
                ProductId = res.ProductId,
                WarehouseId = res.WarehouseId,
                TransactionType = InventoryTransactionType.Dispatch,
                QuantityChange = -res.Quantity,
                BalanceOnHandAfter = inv.QuantityOnHand,
                BalanceReservedAfter = inv.ReservedQuantity,
                ReferenceType = "SalesOrder",
                ReferenceId = salesOrderId.ToString(),
                Notes = $"Order dispatched and physical stock deducted",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };
            await _context.InventoryTransactions.AddAsync(tx, ct);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<InventoryDto> AdjustStockAsync(StockAdjustmentRequest request, string userId = "System", CancellationToken ct = default)
    {
        var inv = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId, ct)
            ?? throw new NotFoundException(nameof(Inventory), $"{request.ProductId}:{request.WarehouseId}");

        var newOnHand = inv.QuantityOnHand + request.QuantityAdjustment;
        if (newOnHand < 0)
        {
            throw new BusinessRuleValidationException($"Adjustment of {request.QuantityAdjustment} would make on-hand stock negative ({newOnHand}).");
        }
        if (newOnHand < inv.ReservedQuantity)
        {
            throw new BusinessRuleValidationException($"Adjustment would leave on-hand ({newOnHand}) less than reserved ({inv.ReservedQuantity}).");
        }

        inv.QuantityOnHand = newOnHand;
        inv.UpdatedAt = DateTime.UtcNow;
        inv.ConcurrencyToken = Guid.NewGuid();

        var tx = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            TransactionType = request.QuantityAdjustment >= 0 ? InventoryTransactionType.Adjustment : InventoryTransactionType.Adjustment,
            QuantityChange = request.QuantityAdjustment,
            BalanceOnHandAfter = inv.QuantityOnHand,
            BalanceReservedAfter = inv.ReservedQuantity,
            ReferenceType = "ManualAdjustment",
            ReferenceId = request.Reason,
            Notes = request.Notes ?? $"Reason: {request.Reason}",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(tx, ct);
        await _context.SaveChangesAsync(ct);

        return new InventoryDto
        {
            Id = inv.Id,
            ProductId = inv.ProductId,
            ProductSku = inv.Product.Sku,
            ProductName = inv.Product.Name,
            WarehouseId = inv.WarehouseId,
            WarehouseCode = inv.Warehouse.Code,
            WarehouseName = inv.Warehouse.Name,
            QuantityOnHand = inv.QuantityOnHand,
            ReservedQuantity = inv.ReservedQuantity,
            DamagedQuantity = inv.DamagedQuantity,
            MinimumStock = inv.MinimumStock,
            UpdatedAt = inv.UpdatedAt
        };
    }

    public async Task<InventoryDto> ReceiveStockAsync(StockReceiptRequest request, string userId = "System", CancellationToken ct = default)
    {
        if (request.QuantityReceived <= 0)
        {
            throw new BusinessRuleValidationException("Received quantity must be positive.");
        }

        var inv = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.WarehouseId == request.WarehouseId, ct);

        if (inv == null)
        {
            var product = await _context.Products.FindAsync(new object[] { request.ProductId }, ct)
                ?? throw new NotFoundException(nameof(Product), request.ProductId);

            inv = new Inventory
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                WarehouseId = request.WarehouseId,
                QuantityOnHand = request.QuantityReceived,
                ReservedQuantity = 0,
                DamagedQuantity = 0,
                MinimumStock = product.ReorderLevel / 2
            };
            await _context.Inventories.AddAsync(inv, ct);
        }
        else
        {
            inv.QuantityOnHand += request.QuantityReceived;
            inv.UpdatedAt = DateTime.UtcNow;
            inv.ConcurrencyToken = Guid.NewGuid();
        }

        var tx = new InventoryTransaction
        {
            Id = Guid.NewGuid(),
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            TransactionType = InventoryTransactionType.Receipt,
            QuantityChange = request.QuantityReceived,
            BalanceOnHandAfter = inv.QuantityOnHand,
            BalanceReservedAfter = inv.ReservedQuantity,
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            Notes = request.Notes ?? "Stock received from supplier",
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _context.InventoryTransactions.AddAsync(tx, ct);
        await _context.SaveChangesAsync(ct);

        return new InventoryDto
        {
            Id = inv.Id,
            ProductId = inv.ProductId,
            ProductSku = inv.Product.Sku,
            ProductName = inv.Product.Name,
            WarehouseId = inv.WarehouseId,
            WarehouseCode = inv.Warehouse.Code,
            WarehouseName = inv.Warehouse.Name,
            QuantityOnHand = inv.QuantityOnHand,
            ReservedQuantity = inv.ReservedQuantity,
            DamagedQuantity = inv.DamagedQuantity,
            MinimumStock = inv.MinimumStock,
            UpdatedAt = inv.UpdatedAt
        };
    }
}
