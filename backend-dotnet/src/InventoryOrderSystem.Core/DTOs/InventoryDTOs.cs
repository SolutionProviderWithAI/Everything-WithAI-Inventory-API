namespace InventoryOrderSystem.Core.DTOs;

public class InventoryDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int DamagedQuantity { get; set; }
    public int MinimumStock { get; set; }
    public int AvailableToSell => QuantityOnHand - ReservedQuantity;
    public DateTime UpdatedAt { get; set; }
}

public class StockAdjustmentRequest
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityAdjustment { get; set; } // Positive to add, negative to reduce
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class StockReceiptRequest
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public int QuantityReceived { get; set; }
    public string ReferenceType { get; set; } = "SupplierReceipt";
    public string? ReferenceId { get; set; }
    public string? Notes { get; set; }
}

public class InventoryMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int BalanceOnHandAfter { get; set; }
    public int BalanceReservedAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LowStockDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableToSell { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    public string Status { get; set; } = string.Empty; // "LowStock", "OutOfStock"
}
