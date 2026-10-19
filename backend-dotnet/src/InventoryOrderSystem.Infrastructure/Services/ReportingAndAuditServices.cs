using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly AppDbContext _context;

    public ReportingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SalesReportDto> GetSalesReportAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var start = startDate ?? DateTime.UtcNow.AddDays(-30);
        var end = endDate ?? DateTime.UtcNow;

        var query = _context.SalesOrders
            .Include(o => o.Items)
            .Where(o => o.OrderDate >= start && o.OrderDate <= end && o.Status != OrderStatus.Cancelled);

        var orders = await query.ToListAsync(ct);

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.GrandTotal);
        var totalDiscount = orders.Sum(o => o.TotalDiscount);
        var totalTax = orders.Sum(o => o.TaxAmount);

        // Daily breakdown
        var daily = orders
            .GroupBy(o => o.OrderDate.ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new DailySalesDataDto
            {
                Date = g.Key,
                OrderCount = g.Count(),
                TotalSales = g.Sum(o => o.GrandTotal)
            })
            .ToList();

        // Top products
        var topProducts = orders
            .SelectMany(o => o.Items)
            .GroupBy(i => new { i.ProductId, i.Sku, i.ProductName })
            .Select(g => new BestSellerDto
            {
                ProductId = g.Key.ProductId,
                Sku = g.Key.Sku,
                ProductName = g.Key.ProductName,
                UnitsSold = g.Sum(i => i.Quantity),
                RevenueGenerated = g.Sum(i => i.LineTotal)
            })
            .OrderByDescending(b => b.UnitsSold)
            .Take(10)
            .ToList();

        // Orders by status (including all orders in period)
        var allInPeriod = await _context.SalesOrders
            .Where(o => o.OrderDate >= start && o.OrderDate <= end)
            .ToListAsync(ct);

        var byStatus = allInPeriod
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrdersByStatusDto
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.GrandTotal)
            })
            .ToList();

        return new SalesReportDto
        {
            PeriodStart = start,
            PeriodEnd = end,
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            TotalDiscount = totalDiscount,
            TotalTax = totalTax,
            DailySales = daily,
            TopProducts = topProducts,
            OrdersByStatus = byStatus
        };
    }

    public async Task<InventoryValuationDto> GetInventoryValuationAsync(CancellationToken ct = default)
    {
        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .Where(i => i.Product.IsActive)
            .ToListAsync(ct);

        var totalActiveProducts = await _context.Products.CountAsync(p => p.IsActive, ct);
        var totalOnHand = inventories.Sum(i => i.QuantityOnHand);
        var totalReserved = inventories.Sum(i => i.ReservedQuantity);
        var totalAvailable = totalOnHand - totalReserved;

        decimal totalCost = inventories.Sum(i => i.QuantityOnHand * i.Product.CostPrice);
        decimal totalRetail = inventories.Sum(i => i.QuantityOnHand * i.Product.SellingPrice);

        var lowStockCount = inventories
            .Where(i => (i.QuantityOnHand - i.ReservedQuantity) <= i.Product.ReorderLevel && (i.QuantityOnHand - i.ReservedQuantity) > 0)
            .Select(i => i.ProductId)
            .Distinct()
            .Count();

        var outOfStockCount = inventories
            .Where(i => (i.QuantityOnHand - i.ReservedQuantity) <= 0)
            .Select(i => i.ProductId)
            .Distinct()
            .Count();

        var warehouses = inventories
            .GroupBy(i => new { i.WarehouseId, i.Warehouse.Name })
            .Select(g => new WarehouseValuationDto
            {
                WarehouseId = g.Key.WarehouseId,
                WarehouseName = g.Key.Name,
                TotalUnits = g.Sum(i => i.QuantityOnHand),
                TotalCostValue = g.Sum(i => i.QuantityOnHand * i.Product.CostPrice)
            })
            .ToList();

        return new InventoryValuationDto
        {
            TotalActiveProducts = totalActiveProducts,
            TotalUnitsOnHand = totalOnHand,
            TotalUnitsReserved = totalReserved,
            TotalUnitsAvailable = totalAvailable,
            TotalCostValuation = totalCost,
            TotalRetailValuation = totalRetail,
            LowStockProductCount = lowStockCount,
            OutOfStockProductCount = outOfStockCount,
            WarehouseBreakdown = warehouses
        };
    }

    public async Task<byte[]> ExportSalesReportCsvAsync(DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var report = await GetSalesReportAsync(startDate, endDate, ct);
        var sb = new StringBuilder();

        sb.AppendLine("Sales Report Summary");
        sb.AppendLine($"Period,{report.PeriodStart:yyyy-MM-dd} to {report.PeriodEnd:yyyy-MM-dd}");
        sb.AppendLine($"Total Orders,{report.TotalOrders}");
        sb.AppendLine($"Total Revenue,₹{report.TotalRevenue:F2}");
        sb.AppendLine($"Total Tax,₹{report.TotalTax:F2}");
        sb.AppendLine($"Average Order Value,₹{report.AverageOrderValue:F2}");
        sb.AppendLine();

        sb.AppendLine("Date,Order Count,Total Sales (INR)");
        foreach (var d in report.DailySales)
        {
            sb.AppendLine($"{d.Date},{d.OrderCount},{d.TotalSales:F2}");
        }

        sb.AppendLine();
        sb.AppendLine("Product SKU,Product Name,Units Sold,Revenue Generated (INR)");
        foreach (var p in report.TopProducts)
        {
            sb.AppendLine($"\"{p.Sku}\",\"{p.ProductName}\",{p.UnitsSold},{p.RevenueGenerated:F2}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportInventoryCsvAsync(CancellationToken ct = default)
    {
        var inventories = await _context.Inventories
            .Include(i => i.Product)
            .Include(i => i.Warehouse)
            .OrderBy(i => i.Product.Name)
            .ToListAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("SKU,Product Name,Warehouse,On Hand,Reserved,Available,Cost Price,Selling Price,Cost Valuation (INR),Retail Valuation (INR),Status");

        foreach (var inv in inventories)
        {
            var avail = inv.QuantityOnHand - inv.ReservedQuantity;
            var costVal = inv.QuantityOnHand * inv.Product.CostPrice;
            var retailVal = inv.QuantityOnHand * inv.Product.SellingPrice;
            var status = avail <= 0 ? "Out of Stock" : (avail <= inv.Product.ReorderLevel ? "Low Stock" : "In Stock");

            sb.AppendLine($"\"{inv.Product.Sku}\",\"{inv.Product.Name}\",\"{inv.Warehouse.Name}\",{inv.QuantityOnHand},{inv.ReservedQuantity},{avail},{inv.Product.CostPrice:F2},{inv.Product.SellingPrice:F2},{costVal:F2},{retailVal:F2},{status}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string entityName, string entityId, string action,
        object? oldValues = null, object? newValues = null,
        string? userId = null, string? userEmail = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            UserId = userId,
            UserEmail = userEmail,
            Timestamp = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(log, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        string? entityName, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.AuditLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName.ToLower() == entityName.Trim().ToLower());
        }

        var total = await query.CountAsync(ct);
        var logs = await query
            .OrderByDescending(a => a.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                Action = a.Action,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                UserId = a.UserId,
                UserEmail = a.UserEmail,
                Timestamp = a.Timestamp
            })
            .ToListAsync(ct);

        return new PagedResult<AuditLogDto>(logs, total, page, pageSize);
    }
}
