using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Infrastructure.Services;

namespace InventoryOrderSystem.Tests;

public class OrderLifecycleAndConcurrencyTests
{
    private AppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private async Task<(Guid productId, Guid customerId)> SeedScenarioData(AppDbContext db)
    {
        var wh = new Warehouse { Id = Guid.NewGuid(), Code = "WH-MAIN", Name = "Central Logistics", IsPrimary = true };
        var cat = new ProductCategory { Id = Guid.NewGuid(), Name = "General", Code = "CAT-GEN" };

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = cat.Id,
            Sku = "PROD-A-001",
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
            QuantityOnHand = 50, // Opening stock: 50 units as in scenario!
            ReservedQuantity = 0
        };

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CustomerCode = "CUST-1001",
            Name = "John Doe",
            Email = "john@example.com",
            Phone = "+91 9876543210"
        };

        var taxSetting = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "tax_rate", SettingValue = "0.05" };
        var shippingThreshold = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_free_threshold", SettingValue = "1000.00" };
        var shippingFlat = new ApplicationSetting { Id = Guid.NewGuid(), SettingKey = "shipping_flat_rate", SettingValue = "50.00" };

        await db.Warehouses.AddAsync(wh);
        await db.ProductCategories.AddAsync(cat);
        await db.Products.AddAsync(product);
        await db.Inventories.AddAsync(inv);
        await db.Customers.AddAsync(customer);
        await db.ApplicationSettings.AddRangeAsync(taxSetting, shippingThreshold, shippingFlat);
        await db.SaveChangesAsync();

        return (product.Id, customer.Id);
    }

    [Fact]
    public async Task EndToEnd_Scenario_AdminCreatesProduct50Units_CustomerOrders10_StockBecomes40_DispatchedConsumed()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var (productId, customerId) = await SeedScenarioData(db);

        var invService = new InventoryService(db);
        var pricingService = new PricingService(db);
        var orderService = new OrderService(db, invService, pricingService);

        // Step 1: Initial stock check -> 50 units available
        var initialStock = await invService.GetAvailableToSellAsync(productId);
        Assert.Equal(50, initialStock);

        // Step 2: Chatbot / Customer calculates price for 10 units
        var calcReq = new OrderCalculationRequest
        {
            CustomerId = customerId,
            Items = new List<OrderCalculationItemRequest> { new() { ProductId = productId, Quantity = 10 } }
        };
        var calcResult = await pricingService.CalculateOrderTotalsAsync(calcReq);

        Assert.Equal(1000.00m, calcResult.Subtotal); // Subtotal = ₹1,000
        Assert.Equal(50.00m, calcResult.TaxAmount);   // 5% Tax = ₹50
        Assert.Equal(0.00m, calcResult.ShippingCharges); // Free shipping above ₹1000
        Assert.Equal(1050.00m, calcResult.GrandTotal);

        // Step 3: Customer confirms order
        var createOrderReq = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 10 } },
            ShippingAddress = "123 Marine Drive, Mumbai",
            IdempotencyKey = "IDEMP-TEST-001"
        };
        var order = await orderService.CreateOrderAsync(createOrderReq);

        Assert.NotNull(order);
        Assert.StartsWith("ORD-", order.OrderNumber);
        Assert.Equal("Confirmed", order.Status);
        Assert.Equal(1000.00m, order.Subtotal);
        Assert.Equal(1050.00m, order.GrandTotal);

        // Step 4: Available stock becomes 40 units (50 on hand - 10 reserved)
        var stockAfterOrder = await invService.GetAvailableToSellAsync(productId);
        Assert.Equal(40, stockAfterOrder);

        var invRecord = await db.Inventories.FirstAsync(i => i.ProductId == productId);
        Assert.Equal(50, invRecord.QuantityOnHand);
        Assert.Equal(10, invRecord.ReservedQuantity);

        // Step 5: Order dispatched -> Reservation consumed, on-hand becomes 40, reserved becomes 0
        var dispatchedOrder = await orderService.DispatchOrderAsync(order.Id);
        Assert.Equal("Dispatched", dispatchedOrder.Status);

        var invRecordAfterDispatch = await db.Inventories.FirstAsync(i => i.ProductId == productId);
        Assert.Equal(40, invRecordAfterDispatch.QuantityOnHand);
        Assert.Equal(0, invRecordAfterDispatch.ReservedQuantity);
        Assert.Equal(40, invRecordAfterDispatch.AvailableToSell);
    }

    [Fact]
    public async Task FailedOrder_100UnitsWhen50Available_ShouldThrowInsufficientStockException_NoOrderConfirmed()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var (productId, customerId) = await SeedScenarioData(db);

        var invService = new InventoryService(db);
        var pricingService = new PricingService(db);
        var orderService = new OrderService(db, invService, pricingService);

        var initialStock = await invService.GetAvailableToSellAsync(productId);
        Assert.Equal(50, initialStock);

        var createOrderReq = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 100 } }, // Requested 100!
            ShippingAddress = "123 High Street"
        };

        // Attempting to order 100 must fail with InsufficientStockException!
        var ex = await Assert.ThrowsAsync<InsufficientStockException>(async () =>
        {
            await orderService.CreateOrderAsync(createOrderReq);
        });

        Assert.Equal(100, ex.RequestedQuantity);
        Assert.Equal(50, ex.AvailableQuantity);

        // Verify stock is untouched
        var stockAfter = await invService.GetAvailableToSellAsync(productId);
        Assert.Equal(50, stockAfter);

        var ordersInDb = await db.SalesOrders.CountAsync();
        Assert.Equal(0, ordersInDb);

        var reservationsInDb = await db.InventoryReservations.CountAsync();
        Assert.Equal(0, reservationsInDb);
    }

    [Fact]
    public async Task IdempotencyKey_ShouldPreventDuplicateOrderCreation()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var (productId, customerId) = await SeedScenarioData(db);

        var invService = new InventoryService(db);
        var pricingService = new PricingService(db);
        var orderService = new OrderService(db, invService, pricingService);

        var key = "IDEMP-DUPLICATE-CHECK-12345";
        var req = new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 5 } },
            IdempotencyKey = key
        };

        // First attempt: creates order
        var order1 = await orderService.CreateOrderAsync(req);
        Assert.Equal(45, await invService.GetAvailableToSellAsync(productId));

        // Duplicate attempt with same key: must return same order and NOT reserve stock twice!
        var order2 = await orderService.CreateOrderAsync(req);

        Assert.Equal(order1.Id, order2.Id);
        Assert.Equal(order1.OrderNumber, order2.OrderNumber);
        Assert.Equal(45, await invService.GetAvailableToSellAsync(productId)); // Still 45, not 40!
    }

    [Fact]
    public async Task CannotCancelOrder_AfterDispatch()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var (productId, customerId) = await SeedScenarioData(db);

        var invService = new InventoryService(db);
        var pricingService = new PricingService(db);
        var orderService = new OrderService(db, invService, pricingService);

        var order = await orderService.CreateOrderAsync(new CreateOrderRequest
        {
            CustomerId = customerId,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 5 } }
        });

        await orderService.DispatchOrderAsync(order.Id);

        // Attempting to cancel dispatched order must fail
        await Assert.ThrowsAsync<BusinessRuleValidationException>(async () =>
        {
            await orderService.CancelOrderAsync(order.Id, "Customer wants cancellation");
        });
    }

    [Fact]
    public async Task ConcurrentReservations_ShouldNeverCauseOverselling()
    {
        var dbName = Guid.NewGuid().ToString();
        var db = CreateDbContext(dbName);
        var (productId, customerId) = await SeedScenarioData(db);

        // Starting with 50 units.
        // We will run 10 parallel threads, each requesting 10 units.
        // Exactly 5 requests can succeed (5 * 10 = 50). The remaining 5 MUST fail with InsufficientStockException.
        // Available stock must end at exactly 0, never negative!

        var successfulOrders = new ConcurrentBag<OrderDto>();
        var failedOrders = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            // Each task has its own DbContext instance pointing to the same in-memory DB to simulate real concurrency
            var taskDb = CreateDbContext(dbName);
            var taskInvService = new InventoryService(taskDb);
            var taskPricingService = new PricingService(taskDb);
            var taskOrderService = new OrderService(taskDb, taskInvService, taskPricingService);

            try
            {
                var ord = await taskOrderService.CreateOrderAsync(new CreateOrderRequest
                {
                    CustomerId = customerId,
                    Items = new List<CreateOrderItemRequest> { new() { ProductId = productId, Quantity = 10 } },
                    IdempotencyKey = $"CONCURRENT-{i}"
                });
                successfulOrders.Add(ord);
            }
            catch (Exception ex)
            {
                failedOrders.Add(ex);
            }
        });

        await Task.WhenAll(tasks);

        // Assert exactly 5 succeeded, 5 failed
        Assert.Equal(5, successfulOrders.Count);
        Assert.Equal(5, failedOrders.Count);

        foreach (var err in failedOrders)
        {
            Assert.IsType<InsufficientStockException>(err);
        }

        // Check final stock in fresh context
        var verifyDb = CreateDbContext(dbName);
        var inv = await verifyDb.Inventories.FirstAsync(i => i.ProductId == productId);
        Assert.Equal(50, inv.QuantityOnHand);
        Assert.Equal(50, inv.ReservedQuantity);
        Assert.Equal(0, inv.AvailableToSell); // Exactly 0, NEVER negative!
    }
}
