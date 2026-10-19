using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Core.Entities;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CreditCard;
    public decimal Amount { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Return : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public string ReturnNumber { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public decimal TotalRefundAmount { get; set; } = 0m;

    public ICollection<ReturnItem> Items { get; set; } = new List<ReturnItem>();
}

public class ReturnItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReturnId { get; set; }
    public Return Return { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public ItemCondition Condition { get; set; } = ItemCondition.Good;
    public bool RestockApproved { get; set; } = false;
}
