using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Core.DTOs;

public class OrderCalculationItemRequest
{
    public Guid? ProductId { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
}

public class OrderCalculationRequest
{
    public Guid? CustomerId { get; set; }
    public List<OrderCalculationItemRequest> Items { get; set; } = new();
    public string? PromoCode { get; set; }
}

public class OrderCalculationItemResult
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int AvailableStock { get; set; }
    public bool HasSufficientStock => AvailableStock >= Quantity;
}

public class OrderCalculationResult
{
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCharges { get; set; }
    public decimal GrandTotal { get; set; }
    public bool AllItemsInStock { get; set; }
    public List<OrderCalculationItemResult> Items { get; set; } = new();
}

public class CreateOrderItemRequest
{
    public Guid? ProductId { get; set; }
    public string? Sku { get; set; }
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public List<CreateOrderItemRequest> Items { get; set; } = new();
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string? Notes { get; set; }
    public string? IdempotencyKey { get; set; }
    public bool AutoConfirm { get; set; } = false;
}

public class OrderItemDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderStatusHistoryDto
{
    public string? PreviousStatus { get; set; }
    public string NewStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

public class OrderDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCharges { get; set; }
    public decimal GrandTotal { get; set; }
    public string? ShippingAddress { get; set; }
    public string? BillingAddress { get; set; }
    public string? Notes { get; set; }
    public string? IdempotencyKey { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
}

public class CancelOrderRequest
{
    public string Reason { get; set; } = "Customer requested cancellation";
}
