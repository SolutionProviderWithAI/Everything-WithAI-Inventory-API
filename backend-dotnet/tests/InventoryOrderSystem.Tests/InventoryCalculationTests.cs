using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Infrastructure.Services;

namespace InventoryOrderSystem.Tests;

public class InventoryCalculationTests
{
    private AppDbContext CreateInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AvailableToSell_ShouldEqual_QuantityOnHand_Minus_ReservedQuantity()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var invService = new InventoryService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test Warehouse", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "Electronics", Code = "CAT-ELEC" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A",
            Name = "Product A",
            SellingPrice = 100.00m,
            CostPrice = 60.00m,
            ReorderLevel = 10,
            ReorderQuantity = 50
        };

        var inv = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 50,
            ReservedQuantity = 10
        };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.SaveChangesAsync();

        var available = await invService.GetAvailableToSellAsync(product.Id);

        Assert.Equal(40, available);
        Assert.Equal(50, inv.QuantityOnHand);
        Assert.Equal(10, inv.ReservedQuantity);
    }

    [Fact]
    public async Task ReservingStock_ShouldReduce_AvailableToSell_And_Increase_ReservedQuantity()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var invService = new InventoryService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test Warehouse", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "General", Code = "CAT-GEN" };
        var product = new Product
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
            ProductId = product.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 50,
            ReservedQuantity = 0
        };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        var success = await invService.ReserveStockAsync(orderId, product.Id, 10);

        Assert.True(success);
        Assert.Equal(50, inv.QuantityOnHand);
        Assert.Equal(10, inv.ReservedQuantity);
        Assert.Equal(40, inv.AvailableToSell);

        // Verify transaction logged
        var tx = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.ProductId == product.Id && t.ReferenceType == "SalesOrder");
        Assert.NotNull(tx);
        Assert.Equal(InventoryTransactionType.Reservation, tx.TransactionType);
        Assert.Equal(10, tx.QuantityChange);
    }

    [Fact]
    public async Task DispathOrder_ShouldDeduct_OnHandStock_And_ConsumeReservation()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var invService = new InventoryService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test Warehouse", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "General", Code = "CAT-GEN" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A-001",
            Name = "Product A",
            SellingPrice = 100.00m
        };

        var inv = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 50,
            ReservedQuantity = 0
        };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        await invService.ReserveStockAsync(orderId, product.Id, 10);
        Assert.Equal(50, inv.QuantityOnHand);
        Assert.Equal(10, inv.ReservedQuantity);

        // Dispatch
        await invService.FulfillReservationAndDeductStockAsync(orderId);

        Assert.Equal(40, inv.QuantityOnHand);
        Assert.Equal(0, inv.ReservedQuantity);
        Assert.Equal(40, inv.AvailableToSell);

        // Verify dispatch transaction recorded
        var dispatchTx = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.TransactionType == InventoryTransactionType.Dispatch);
        Assert.NotNull(dispatchTx);
        Assert.Equal(-10, dispatchTx.QuantityChange);
        Assert.Equal(40, dispatchTx.BalanceOnHandAfter);
    }

    [Fact]
    public async Task CancelOrder_ShouldRelease_Reservation_RestoringAvailableStock()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var invService = new InventoryService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test Warehouse", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "General", Code = "CAT-GEN" };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A-001",
            Name = "Product A",
            SellingPrice = 100.00m
        };

        var inv = new Inventory
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            WarehouseId = wh.Id,
            QuantityOnHand = 50,
            ReservedQuantity = 0
        };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.SaveChangesAsync();

        var orderId = Guid.NewGuid();
        await invService.ReserveStockAsync(orderId, product.Id, 10);
        Assert.Equal(40, inv.AvailableToSell);

        // Cancel
        await invService.ReleaseReservationAsync(orderId, "Customer decided not to proceed");

        Assert.Equal(50, inv.QuantityOnHand);
        Assert.Equal(0, inv.ReservedQuantity);
        Assert.Equal(50, inv.AvailableToSell);

        var releaseTx = await db.InventoryTransactions.FirstOrDefaultAsync(t => t.TransactionType == InventoryTransactionType.ReservationRelease);
        Assert.NotNull(releaseTx);
    }

    [Fact]
    public async Task AdjustStock_NegativeBeyondOnHand_ShouldThrowValidationException()
    {
        var db = CreateInMemoryDbContext(Guid.NewGuid().ToString());
        var invService = new InventoryService(db);

        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-TEST", Name = "Test Warehouse", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "General", Code = "CAT-GEN" };
        var product = new Product { Id = Guid.NewGuid(), CategoryId = cat.Id, Sku = "P1", Name = "P1", SellingPrice = 50m };
        var inv = new Inventory { Id = Guid.NewGuid(), ProductId = product.Id, WarehouseId = wh.Id, QuantityOnHand = 10, ReservedQuantity = 0 };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.SaveChangesAsync();

        var adjustReq = new StockAdjustmentRequest
        {
            ProductId = product.Id,
            WarehouseId = wh.Id,
            QuantityAdjustment = -20, // Cannot drop 10 down by 20 to -10
            Reason = "Damaged goods"
        };

        await Assert.ThrowsAsync<BusinessRuleValidationException>(async () =>
        {
            await invService.AdjustStockAsync(adjustReq);
        });
    }
}
