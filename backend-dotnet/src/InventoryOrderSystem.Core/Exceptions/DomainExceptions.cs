namespace InventoryOrderSystem.Core.Exceptions;

public class InsufficientStockException : Exception
{
    public Guid ProductId { get; }
    public string Sku { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(Guid productId, string sku, int requested, int available)
        : base($"Insufficient stock for product '{sku}' (ID: {productId}). Requested: {requested}, Available to sell: {available}.")
    {
        ProductId = productId;
        Sku = sku;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}

public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"Entity '{entityName}' with identifier '{key}' was not found.")
    {
    }
}

public class BusinessRuleValidationException : Exception
{
    public BusinessRuleValidationException(string message) : base(message)
    {
    }
}

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message)
    {
    }
}

public class DuplicateOrderException : Exception
{
    public string IdempotencyKey { get; }
    public string OrderNumber { get; }

    public DuplicateOrderException(string idempotencyKey, string orderNumber)
        : base($"An order with idempotency key '{idempotencyKey}' already exists: {orderNumber}.")
    {
        IdempotencyKey = idempotencyKey;
        OrderNumber = orderNumber;
    }
}
