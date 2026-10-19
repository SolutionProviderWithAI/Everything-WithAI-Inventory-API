using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Core.Entities;

public class Customer : BaseEntity
{
    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? TaxNumber { get; set; }
    public decimal CreditLimit { get; set; } = 0m;
    public decimal OutstandingBalance { get; set; } = 0m;
    public bool IsActive { get; set; } = true;

    public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();
    public ICollection<SalesOrder> Orders { get; set; } = new List<SalesOrder>();
}

public class CustomerAddress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public AddressType AddressType { get; set; } = AddressType.Both;
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "India";
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
