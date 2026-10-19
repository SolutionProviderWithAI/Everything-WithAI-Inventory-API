using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<InventoryReservation> InventoryReservations => Set<InventoryReservation>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierProduct> SupplierProducts => Set<SupplierProduct>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderItem> SalesOrderItems => Set<SalesOrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnItem> ReturnItems => Set<ReturnItem>();
    public DbSet<ChatbotConversation> ChatbotConversations => Set<ChatbotConversation>();
    public DbSet<ChatbotMessage> ChatbotMessages => Set<ChatbotMessage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ApplicationSetting> ApplicationSettings => Set<ApplicationSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. Roles
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
        });

        // 2. Users
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });

        // 3. Customers
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerCode).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Phone).IsRequired().HasMaxLength(30);
            entity.Property(e => e.CreditLimit).HasPrecision(14, 2);
            entity.Property(e => e.OutstandingBalance).HasPrecision(14, 2);
            entity.HasIndex(e => e.CustomerCode).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Phone);
            entity.HasOne(e => e.User)
                  .WithOne(u => u.CustomerProfile)
                  .HasForeignKey<Customer>(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 4. Customer Addresses
        modelBuilder.Entity<CustomerAddress>(entity =>
        {
            entity.ToTable("customer_addresses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Line1).IsRequired().HasMaxLength(255);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.State).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PostalCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AddressType).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Addresses)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 5. Product Categories
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.HasOne(e => e.ParentCategory)
                  .WithMany(p => p.SubCategories)
                  .HasForeignKey(e => e.ParentCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 6. Products
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CostPrice).HasPrecision(14, 2);
            entity.Property(e => e.SellingPrice).HasPrecision(14, 2);
            entity.HasIndex(e => e.Sku).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });

        // 7. Warehouses
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // 8. Inventory
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.ToTable("inventory");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ProductId, e.WarehouseId }).IsUnique();
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.Inventories)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse)
                  .WithMany(w => w.Inventories)
                  .HasForeignKey(e => e.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });

        // 9. Sales Orders
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("sales_orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.PaymentStatus).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.Subtotal).HasPrecision(14, 2);
            entity.Property(e => e.TotalDiscount).HasPrecision(14, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(14, 2);
            entity.Property(e => e.ShippingCharges).HasPrecision(14, 2);
            entity.Property(e => e.GrandTotal).HasPrecision(14, 2);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OrderDate);
            entity.HasOne(e => e.Customer)
                  .WithMany(c => c.Orders)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.ConcurrencyToken).IsConcurrencyToken();
        });

        // 10. Sales Order Items
        modelBuilder.Entity<SalesOrderItem>(entity =>
        {
            entity.ToTable("sales_order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sku).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProductName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitPrice).HasPrecision(14, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(14, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(14, 2);
            entity.Property(e => e.LineTotal).HasPrecision(14, 2);
            entity.HasOne(e => e.SalesOrder)
                  .WithMany(o => o.Items)
                  .HasForeignKey(e => e.SalesOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.SalesOrderItems)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 11. Inventory Reservations
        modelBuilder.Entity<InventoryReservation>(entity =>
        {
            entity.ToTable("inventory_reservations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(e => e.SalesOrder)
                  .WithMany(o => o.Reservations)
                  .HasForeignKey(e => e.SalesOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse)
                  .WithMany()
                  .HasForeignKey(e => e.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. Inventory Transactions
        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.ToTable("inventory_transactions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionType).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.ReferenceType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ReferenceId).HasMaxLength(100);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.ReferenceType, e.ReferenceId });
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse)
                  .WithMany()
                  .HasForeignKey(e => e.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. Suppliers
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.HasIndex(e => e.Code).IsUnique();
        });

        // 14. Supplier Products
        modelBuilder.Entity<SupplierProduct>(entity =>
        {
            entity.ToTable("supplier_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CostPrice).HasPrecision(14, 2);
            entity.HasIndex(e => new { e.SupplierId, e.ProductId }).IsUnique();
            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.SupplierProducts)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany(p => p.SupplierProducts)
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 15. Purchase Orders
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(40);
            entity.Property(e => e.TotalAmount).HasPrecision(14, 2);
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.HasOne(e => e.Supplier)
                  .WithMany(s => s.PurchaseOrders)
                  .HasForeignKey(e => e.SupplierId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Warehouse)
                  .WithMany()
                  .HasForeignKey(e => e.WarehouseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 16. Purchase Order Items
        modelBuilder.Entity<PurchaseOrderItem>(entity =>
        {
            entity.ToTable("purchase_order_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitCost).HasPrecision(14, 2);
            entity.Property(e => e.LineTotal).HasPrecision(14, 2);
            entity.HasOne(e => e.PurchaseOrder)
                  .WithMany(p => p.Items)
                  .HasForeignKey(e => e.PurchaseOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 17. Order Status History
        modelBuilder.Entity<OrderStatusHistory>(entity =>
        {
            entity.ToTable("order_status_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.NewStatus).IsRequired().HasMaxLength(40);
            entity.HasOne(e => e.SalesOrder)
                  .WithMany(o => o.StatusHistory)
                  .HasForeignKey(e => e.SalesOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 18. Payments
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(14, 2);
            entity.Property(e => e.PaymentMethod).HasConversion<string>().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.HasOne(e => e.SalesOrder)
                  .WithMany(o => o.Payments)
                  .HasForeignKey(e => e.SalesOrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 19. Returns
        modelBuilder.Entity<Return>(entity =>
        {
            entity.ToTable("returns");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNumber).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(30);
            entity.Property(e => e.TotalRefundAmount).HasPrecision(14, 2);
            entity.HasIndex(e => e.ReturnNumber).IsUnique();
            entity.HasOne(e => e.SalesOrder)
                  .WithMany(o => o.Returns)
                  .HasForeignKey(e => e.SalesOrderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 20. Return Items
        modelBuilder.Entity<ReturnItem>(entity =>
        {
            entity.ToTable("return_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Condition).HasConversion<string>().HasMaxLength(50);
            entity.HasOne(e => e.Return)
                  .WithMany(r => r.Items)
                  .HasForeignKey(e => e.ReturnId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                  .WithMany()
                  .HasForeignKey(e => e.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 21. Chatbot Conversations
        modelBuilder.Entity<ChatbotConversation>(entity =>
        {
            entity.ToTable("chatbot_conversations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasOne(e => e.Customer)
                  .WithMany()
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 22. Chatbot Messages
        modelBuilder.Entity<ChatbotMessage>(entity =>
        {
            entity.ToTable("chatbot_messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Sender).IsRequired().HasMaxLength(20);
            entity.Property(e => e.MessageText).IsRequired();
            entity.HasIndex(e => e.CreatedAt);
            entity.HasOne(e => e.Conversation)
                  .WithMany(c => c.Messages)
                  .HasForeignKey(e => e.ConversationId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 23. Audit Logs
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => new { e.EntityName, e.EntityId });
            entity.HasIndex(e => e.Timestamp);
        });

        // 24. Application Settings
        modelBuilder.Entity<ApplicationSetting>(entity =>
        {
            entity.ToTable("application_settings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SettingKey).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.SettingKey).IsUnique();
        });
    }
}
