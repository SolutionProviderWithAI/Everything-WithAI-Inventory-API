using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;
    private readonly IPricingService _pricingService;

    public OrderService(
        AppDbContext context,
        IInventoryService inventoryService,
        IPricingService pricingService)
    {
        _context = context;
        _inventoryService = inventoryService;
        _pricingService = pricingService;
    }

    public async Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, string userId = "System", CancellationToken ct = default)
    {
        // 1. Idempotency Check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Items)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.IdempotencyKey == request.IdempotencyKey, ct);

            if (existing != null)
            {
                return MapToDto(existing);
            }
        }

        if (request.Items == null || !request.Items.Any())
        {
            throw new BusinessRuleValidationException("Order must have at least one line item.");
        }

        // 2. Resolve Customer
        Customer? customer = null;
        if (request.CustomerId.HasValue)
        {
            customer = await _context.Customers.FindAsync(new object[] { request.CustomerId.Value }, ct);
        }

        if (customer == null && !string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == request.CustomerPhone, ct);
        }

        if (customer == null)
        {
            // Auto-create customer profile for guest/chatbot checkout
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                CustomerCode = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                Name = string.IsNullOrWhiteSpace(request.CustomerName) ? "Customer Guest" : request.CustomerName,
                Email = string.IsNullOrWhiteSpace(request.CustomerEmail) ? $"customer_{Random.Shared.Next(1000, 9999)}@example.com" : request.CustomerEmail,
                Phone = string.IsNullOrWhiteSpace(request.CustomerPhone) ? "+91 9999999999" : request.CustomerPhone,
                CreditLimit = 50000m,
                OutstandingBalance = 0m,
                IsActive = true
            };
            await _context.Customers.AddAsync(customer, ct);
            await _context.SaveChangesAsync(ct);
        }

        // 3. Server-side Pricing and Validation
        var calcReq = new OrderCalculationRequest
        {
            CustomerId = customer.Id,
            Items = request.Items.Select(i => new OrderCalculationItemRequest
            {
                ProductId = i.ProductId,
                Sku = i.Sku,
                Quantity = i.Quantity
            }).ToList()
        };

        var calcResult = await _pricingService.CalculateOrderTotalsAsync(calcReq, ct);

        // Pre-check stock
        foreach (var calcItem in calcResult.Items)
        {
            if (calcItem.AvailableStock < calcItem.Quantity)
            {
                throw new InsufficientStockException(calcItem.ProductId, calcItem.Sku, calcItem.Quantity, calcItem.AvailableStock);
            }
        }

        // 4. Generate Unique Order Number
        var orderDate = DateTime.UtcNow;
        var orderNumber = $"ORD-{orderDate:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var initialStatus = request.AutoConfirm ? OrderStatus.Confirmed : OrderStatus.Confirmed; // default to Confirmed

        var salesOrder = new SalesOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            OrderDate = orderDate,
            Status = initialStatus,
            PaymentStatus = PaymentStatus.Pending,
            Subtotal = calcResult.Subtotal,
            TotalDiscount = calcResult.TotalDiscount,
            TaxAmount = calcResult.TaxAmount,
            ShippingCharges = calcResult.ShippingCharges,
            GrandTotal = calcResult.GrandTotal,
            ShippingAddress = request.ShippingAddress ?? "Default Customer Address",
            BillingAddress = request.BillingAddress ?? request.ShippingAddress ?? "Default Customer Address",
            Notes = request.Notes,
            IdempotencyKey = request.IdempotencyKey,
            CreatedBy = userId,
            CreatedAt = orderDate,
            UpdatedAt = orderDate
        };

        foreach (var item in calcResult.Items)
        {
            salesOrder.Items.Add(new SalesOrderItem
            {
                Id = Guid.NewGuid(),
                SalesOrderId = salesOrder.Id,
                ProductId = item.ProductId,
                Sku = item.Sku,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxAmount = Math.Round(item.LineTotal * calcResult.TaxRate, 2),
                LineTotal = item.LineTotal,
                CreatedAt = orderDate
            });
        }

        salesOrder.StatusHistory.Add(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            SalesOrderId = salesOrder.Id,
            PreviousStatus = null,
            NewStatus = initialStatus.ToString(),
            Reason = "Order created and stock reserved",
            ChangedBy = userId,
            ChangedAt = orderDate
        });

        // 5. Database Transaction for Order + Inventory Reservations
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _context.SalesOrders.AddAsync(salesOrder, ct);
            await _context.SaveChangesAsync(ct);

            // Reserve stock for every item
            foreach (var item in salesOrder.Items)
            {
                await _inventoryService.ReserveStockAsync(salesOrder.Id, item.ProductId, item.Quantity, null, ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        // Load relations and return DTO
        return await GetOrderByIdAsync(salesOrder.Id, ct);
    }

    public async Task<OrderDto> ConfirmOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        if (order.Status != OrderStatus.Draft && order.Status != OrderStatus.PendingConfirmation)
        {
            return MapToDto(order); // already confirmed or further
        }

        var prevStatus = order.Status.ToString();
        order.Status = OrderStatus.Confirmed;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = userId;

        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            SalesOrderId = order.Id,
            PreviousStatus = prevStatus,
            NewStatus = OrderStatus.Confirmed.ToString(),
            Reason = "Customer confirmed order",
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        }, ct);

        await _context.SaveChangesAsync(ct);
        return MapToDto(order);
    }

    public async Task<OrderDto> DispatchOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        if (order.Status == OrderStatus.Dispatched || order.Status == OrderStatus.Delivered)
        {
            return MapToDto(order);
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleValidationException("Cannot dispatch a cancelled order.");
        }

        // Transaction to deduct physical stock and update status
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            await _inventoryService.FulfillReservationAndDeductStockAsync(orderId, userId, ct);

            var prev = order.Status.ToString();
            order.Status = OrderStatus.Dispatched;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = userId;

            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                PreviousStatus = prev,
                NewStatus = OrderStatus.Dispatched.ToString(),
                Reason = "Order dispatched from warehouse",
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return MapToDto(order);
    }

    public async Task<OrderDto> DeliverOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new BusinessRuleValidationException("Cannot deliver a cancelled order.");
        }

        var prev = order.Status.ToString();
        order.Status = OrderStatus.Delivered;
        order.PaymentStatus = PaymentStatus.Paid;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = userId;

        await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
        {
            Id = Guid.NewGuid(),
            SalesOrderId = order.Id,
            PreviousStatus = prev,
            NewStatus = OrderStatus.Delivered.ToString(),
            Reason = "Order delivered to recipient",
            ChangedBy = userId,
            ChangedAt = DateTime.UtcNow
        }, ct);

        await _context.SaveChangesAsync(ct);
        return MapToDto(order);
    }

    public async Task<OrderDto> CancelOrderAsync(Guid orderId, string reason, string userId = "System", CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), orderId);

        if (order.Status == OrderStatus.Dispatched || order.Status == OrderStatus.Delivered)
        {
            throw new BusinessRuleValidationException(
                $"Cannot cancel order '{order.OrderNumber}' because it has already been {order.Status}. Only pending or confirmed orders prior to dispatch can be cancelled.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return MapToDto(order);
        }

        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            // Release reservations
            await _inventoryService.ReleaseReservationAsync(orderId, reason, userId, ct);

            var prev = order.Status.ToString();
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            order.UpdatedBy = userId;

            await _context.OrderStatusHistories.AddAsync(new OrderStatusHistory
            {
                Id = Guid.NewGuid(),
                SalesOrderId = order.Id,
                PreviousStatus = prev,
                NewStatus = OrderStatus.Cancelled.ToString(),
                Reason = reason,
                ChangedBy = userId,
                ChangedAt = DateTime.UtcNow
            }, ct);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }

        return MapToDto(order);
    }

    public async Task<OrderDto> GetOrderByIdAsync(Guid id, CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new NotFoundException(nameof(SalesOrder), id);

        return MapToDto(order);
    }

    public async Task<OrderDto> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        var order = await _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber.ToLower() == orderNumber.Trim().ToLower(), ct)
            ?? throw new NotFoundException(nameof(SalesOrder), orderNumber);

        return MapToDto(order);
    }

    public async Task<PagedResult<OrderDto>> GetOrdersAsync(
        Guid? customerId, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.SalesOrders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == customerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            query = query.Where(o => o.Status == parsedStatus);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.OrderDate >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.OrderDate <= toDate.Value);
        }

        var total = await query.CountAsync(ct);
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(MapToDto).ToList();
        return new PagedResult<OrderDto>(dtos, total, page, pageSize);
    }

    private static OrderDto MapToDto(SalesOrder order)
    {
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name ?? string.Empty,
            CustomerEmail = order.Customer?.Email ?? string.Empty,
            CustomerPhone = order.Customer?.Phone ?? string.Empty,
            OrderDate = order.OrderDate,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            Subtotal = order.Subtotal,
            TotalDiscount = order.TotalDiscount,
            TaxAmount = order.TaxAmount,
            ShippingCharges = order.ShippingCharges,
            GrandTotal = order.GrandTotal,
            ShippingAddress = order.ShippingAddress,
            BillingAddress = order.BillingAddress,
            Notes = order.Notes,
            IdempotencyKey = order.IdempotencyKey,
            CreatedBy = order.CreatedBy,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                Sku = i.Sku,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal
            }).ToList(),
            StatusHistory = order.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new OrderStatusHistoryDto
                {
                    PreviousStatus = h.PreviousStatus,
                    NewStatus = h.NewStatus,
                    Reason = h.Reason,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt
                }).ToList()
        };
    }
}
