using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Infrastructure.Services;

namespace InventoryOrderSystem.Tests;

public class PricingCalculationTests
{
    private AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CalculateOrderTotals_10UnitsOfProductA_ShouldProduce1000Subtotal_50Tax_ZeroShipping()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var pricingService = new PricingService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test WH", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "Gen", Code = "GEN" };
        var productA = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A-001",
            Name = "Product A",
            SellingPrice = 100.00m,
            CostPrice = 60.00m
        };

        var inv = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = productA.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 50,
            ReservedQuantity = 0
        };

        var taxSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "tax_rate", SettingValue = "0.05" };
        var freeShippingSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_free_threshold", SettingValue = "1000.00" };
        var flatShippingSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_flat_rate", SettingValue = "50.00" };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(productA);
        await db.Inventories.AddAsync(inv);
        await db.ApplicationSettings.AddRangeAsync(taxSetting, freeShippingSetting, flatShippingSetting);
        await db.SaveChangesAsync();

        var request = new OrderCalculationRequest
        {
            Items = new List<OrderCalculationItemRequest>
            {
                new() { ProductId = productA.Id, Quantity = 10 }
            }
        };

        var result = await pricingService.CalculateOrderTotalsAsync(request);

        // Assert 10 * 100 = 1000
        Assert.Equal(1000.00m, result.Subtotal);
        // Tax 5% of 1000 = 50
        Assert.Equal(50.00m, result.TaxAmount);
        // Free shipping because subtotal >= 1000
        Assert.Equal(0.00m, result.ShippingCharges);
        // Grand total = 1000 + 50 + 0 = 1050
        Assert.Equal(1050.00m, result.GrandTotal);
        Assert.True(result.AllItemsInStock);
        Assert.Single(result.Items);
        Assert.Equal(100.00m, result.Items[0].UnitPrice);
        Assert.Equal(1000.00m, result.Items[0].LineTotal);
    }

    [Fact]
    public async Task CalculateOrderTotals_SubtotalUnderThreshold_ShouldIncludeFlatShipping()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var pricingService = new PricingService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test WH", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "Gen", Code = "GEN" };
        var productA = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A-001",
            Name = "Product A",
            SellingPrice = 100.00m
        };

        var inv = new Inventory { Id = Guid.NewGuid(), ProductId = productA.Id, WarehouseId = wh.Id, QuantityOnHand = 50, ReservedQuantity = 0 };

        var taxSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "tax_rate", SettingValue = "0.05" };
        var freeShippingSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_free_threshold", SettingValue = "1000.00" };
        var flatShippingSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_flat_rate", SettingValue = "50.00" };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(productA);
        await db.Inventories.AddAsync(inv);
        await db.ApplicationSettings.AddRangeAsync(taxSetting, freeShippingSetting, flatShippingSetting);
        await db.SaveChangesAsync();

        var request = new OrderCalculationRequest
        {
            Items = new List<OrderCalculationItemRequest>
            {
                new() { ProductId = productA.Id, Quantity = 5 } // 5 * 100 = 500
            }
        };

        var result = await pricingService.CalculateOrderTotalsAsync(request);

        Assert.Equal(500.00m, result.Subtotal);
        Assert.Equal(25.00m, result.TaxAmount); // 5%
        Assert.Equal(50.00m, result.ShippingCharges); // flat fee applied
        Assert.Equal(575.00m, result.GrandTotal);
    }
}
