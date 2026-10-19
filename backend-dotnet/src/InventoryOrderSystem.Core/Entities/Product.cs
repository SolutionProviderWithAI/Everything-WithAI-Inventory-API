namespace InventoryOrderSystem.Core.Entities;

public class ProductCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public ProductCategory? ParentCategory { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductCategory> SubCategories { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Product : BaseEntity
{
    public Guid CategoryId { get; set; }
    public ProductCategory Category { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string UnitOfMeasurement { get; set; } = "Unit";
    public decimal CostPrice { get; set; } = 0m;
    public decimal SellingPrice { get; set; } = 0m;
    public int ReorderLevel { get; set; } = 10;
    public int ReorderQuantity { get; set; } = 50;
    public bool IsActive { get; set; } = true;
    public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();

    public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
    public ICollection<SupplierProduct> SupplierProducts { get; set; } = new List<SupplierProduct>();
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}
