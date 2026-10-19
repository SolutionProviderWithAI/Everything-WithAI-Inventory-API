using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Core.Entities;

public class Warehouse
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Address { get; set; }
    public bool IsPrimary { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
}

public class Inventory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int QuantityOnHand { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    public int DamagedQuantity { get; set; } = 0;
    public int MinimumStock { get; set; } = 5;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int AvailableToSell => QuantityOnHand - ReservedQuantity;
}

public class InventoryReservation : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public int Quantity { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Active;
    public DateTime? ExpiresAt { get; set; }
}

public class InventoryTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;
    public InventoryTransactionType TransactionType { get; set; }
    public int QuantityChange { get; set; }
    public int BalanceOnHandAfter { get; set; }
    public int BalanceReservedAfter { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
    public string? Notes { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
