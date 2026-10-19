using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Infrastructure.Services;

public class PricingService : IPricingService
{
    private readonly AppDbContext _context;

    public PricingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetCurrentTaxRateAsync(CancellationToken ct = default)
    {
        var setting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "tax_rate", ct);

        if (setting != null && decimal.TryParse(setting.SettingValue, out var rate))
        {
            return rate;
        }

        return 0.05m; // 5% default GST/VAT
    }

    public async Task<decimal> CalculateShippingChargesAsync(decimal subtotal, CancellationToken ct = default)
    {
        var thresholdSetting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "shipping_free_threshold", ct);
        var flatRateSetting = await _context.ApplicationSettings
            .FirstOrDefaultAsync(s => s.SettingKey == "shipping_flat_rate", ct);

        decimal threshold = 1000m;
        if (thresholdSetting != null && decimal.TryParse(thresholdSetting.SettingValue, out var t))
        {
            threshold = t;
        }

        decimal flatRate = 50m;
        if (flatRateSetting != null && decimal.TryParse(flatRateSetting.SettingValue, out var r))
        {
            flatRate = r;
        }

        return subtotal >= threshold ? 0m : flatRate;
    }

    public async Task<OrderCalculationResult> CalculateOrderTotalsAsync(OrderCalculationRequest request, CancellationToken ct = default)
    {
        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleValidationException("Order must contain at least one item to calculate totals.");
        }

        var result = new OrderCalculationResult();
        decimal subtotal = 0m;
        bool allInStock = true;

        foreach (var reqItem in request.Items)
        {
            if (reqItem.Quantity <= 0)
            {
                throw new BusinessRuleValidationException($"Quantity for product must be greater than zero.");
            }

            Product? product = null;
            if (reqItem.ProductId.HasValue)
            {
                product = await _context.Products
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Id == reqItem.ProductId.Value, ct);
            }
            else if (!string.IsNullOrWhiteSpace(reqItem.Sku))
            {
                var sku = reqItem.Sku.Trim();
                product = await _context.Products
                    .Include(p => p.Inventories)
                    .FirstOrDefaultAsync(p => p.Sku.ToLower() == sku.ToLower() || p.Name.ToLower() == sku.ToLower(), ct);
            }

            if (product == null)
            {
                throw new NotFoundException(nameof(Product), reqItem.ProductId?.ToString() ?? reqItem.Sku ?? "unknown");
            }

            var onHand = product.Inventories.Sum(i => i.QuantityOnHand);
            var reserved = product.Inventories.Sum(i => i.ReservedQuantity);
            var available = onHand - reserved;

            var lineSubtotal = product.SellingPrice * reqItem.Quantity;
            subtotal += lineSubtotal;

            var inStock = available >= reqItem.Quantity;
            if (!inStock)
            {
                allInStock = false;
            }

            result.Items.Add(new OrderCalculationItemResult
            {
                ProductId = product.Id,
                Sku = product.Sku,
                ProductName = product.Name,
                Quantity = reqItem.Quantity,
                UnitPrice = product.SellingPrice,
                DiscountAmount = 0m,
                TaxAmount = 0m,
                LineTotal = lineSubtotal,
                AvailableStock = available
            });
        }

        var taxRate = await GetCurrentTaxRateAsync(ct);
        var taxAmount = Math.Round(subtotal * taxRate, 2);
        var shipping = await CalculateShippingChargesAsync(subtotal, ct);
        var grandTotal = Math.Round(subtotal + taxAmount + shipping, 2);

        result.Subtotal = subtotal;
        result.TotalDiscount = 0m;
        result.TaxRate = taxRate;
        result.TaxAmount = taxAmount;
        result.ShippingCharges = shipping;
        result.GrandTotal = grandTotal;
        result.AllItemsInStock = allInStock;

        return result;
    }
}
