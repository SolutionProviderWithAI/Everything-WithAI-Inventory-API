namespace InventoryOrderSystem.Core.Enums;

public enum OrderStatus
{
    Draft,
    PendingConfirmation,
    Confirmed,
    Processing,
    Dispatched,
    Delivered,
    Cancelled,
    Returned
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}

public enum PaymentMethod
{
    CreditCard,
    DebitCard,
    BankTransfer,
    UPI,
    CashOnDelivery
}

public enum InventoryTransactionType
{
    Receipt,
    Adjustment,
    Reservation,
    ReservationRelease,
    Dispatch,
    Return,
    Damage
}

public enum ReservationStatus
{
    Active,
    Fulfilled,
    Cancelled
}

public enum AddressType
{
    Billing,
    Shipping,
    Both
}

public enum PurchaseOrderStatus
{
    Draft,
    Submitted,
    PartiallyReceived,
    Received,
    Cancelled
}

public enum ReturnStatus
{
    Pending,
    Approved,
    Rejected,
    Refunded
}

public enum ItemCondition
{
    Good,
    Damaged,
    Unopened
}
