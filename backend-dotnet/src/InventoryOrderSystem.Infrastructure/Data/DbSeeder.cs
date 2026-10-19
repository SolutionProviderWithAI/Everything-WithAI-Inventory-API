using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;

namespace InventoryOrderSystem.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Roles
        if (!await context.Roles.AnyAsync())
        {
            var adminRole = new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Full administrative and system control"
            };
            var staffRole = new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "Staff",
                Description = "Inventory and order fulfillment operations"
            };
            var customerRole = new Role
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Customer",
                Description = "Customer ordering and self-service account"
            };

            await context.Roles.AddRangeAsync(adminRole, staffRole, customerRole);
            await context.SaveChangesAsync();
        }

        // 2. Default Users
        if (!await context.Users.AnyAsync())
        {
            var adminRoleId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var staffRoleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            var customerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");

            // Hash with BCrypt
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
            var staffPasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff@123");
            var customerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Customer@123");

            var adminUser = new User
            {
                Id = Guid.Parse("aaaa1111-0000-0000-0000-000000000001"),
                Username = "admin",
                Email = "admin@inventorysystem.com",
                PasswordHash = adminPasswordHash,
                FullName = "System Administrator",
                PhoneNumber = "+91 9876543210",
                RoleId = adminRoleId,
                IsActive = true
            };

            var staffUser = new User
            {
                Id = Guid.Parse("bbbb2222-0000-0000-0000-000000000002"),
                Username = "staff",
                Email = "staff@inventorysystem.com",
                PasswordHash = staffPasswordHash,
                FullName = "Warehouse Operator",
                PhoneNumber = "+91 9876543211",
                RoleId = staffRoleId,
                IsActive = true
            };

            var customerUser = new User
            {
                Id = Guid.Parse("cccc3333-0000-0000-0000-000000000003"),
                Username = "customer",
                Email = "customer@example.com",
                PasswordHash = customerPasswordHash,
                FullName = "John Doe Customer",
                PhoneNumber = "+91 9876543212",
                RoleId = customerRoleId,
                IsActive = true
            };

            await context.Users.AddRangeAsync(adminUser, staffUser, customerUser);
            await context.SaveChangesAsync();
        }

        // 3. Customers
        if (!await context.Customers.AnyAsync())
        {
            var customerUserId = Guid.Parse("cccc3333-0000-0000-0000-000000000003");
            var customer1 = new Customer
            {
                Id = Guid.Parse("c0000001-0000-0000-0000-000000000001"),
                UserId = customerUserId,
                CustomerCode = "CUST-1001",
                Name = "John Doe",
                Email = "customer@example.com",
                Phone = "+91 9876543212",
                CompanyName = "Acme Trading Co.",
                TaxNumber = "GSTIN27AAAAA0000A1Z5",
                CreditLimit = 50000m,
                OutstandingBalance = 0m,
                IsActive = true
            };

            var customer2 = new Customer
            {
                Id = Guid.Parse("c0000002-0000-0000-0000-000000000002"),
                UserId = null,
                CustomerCode = "CUST-1002",
                Name = "Sarah Connor",
                Email = "sarah.connor@cyberdyne.io",
                Phone = "+91 9876543299",
                CompanyName = "Cyberdyne Systems",
                TaxNumber = "GSTIN27BBBBB1111B1Z2",
                CreditLimit = 100000m,
                OutstandingBalance = 0m,
                IsActive = true
            };

            await context.Customers.AddRangeAsync(customer1, customer2);
            await context.SaveChangesAsync();

            var addr1 = new CustomerAddress
            {
                Id = Guid.Parse("ca000001-0000-0000-0000-000000000001"),
                CustomerId = customer1.Id,
                AddressType = AddressType.Both,
                Line1 = "Flat 402, Lotus Grandeur, Andheri East",
                Line2 = "Near Metro Station",
                City = "Mumbai",
                State = "Maharashtra",
                PostalCode = "400069",
                Country = "India",
                IsDefault = true
            };

            var addr2 = new CustomerAddress
            {
                Id = Guid.Parse("ca000002-0000-0000-0000-000000000002"),
                CustomerId = customer2.Id,
                AddressType = AddressType.Shipping,
                Line1 = "Plot 18, Electronic City Phase 1",
                Line2 = "Hosur Road",
                City = "Bengaluru",
                State = "Karnataka",
                PostalCode = "560100",
                Country = "India",
                IsDefault = true
            };

            await context.CustomerAddresses.AddRangeAsync(addr1, addr2);
            await context.SaveChangesAsync();
        }

        // 4. Warehouses
        if (!await context.Warehouses.AnyAsync())
        {
            var wh1 = new Warehouse
            {
                Id = Guid.Parse("b0000001-0000-0000-0000-000000000001"),
                Code = "WH-MAIN",
                Name = "Central Logistics Hub",
                Location = "Bhiwandi, Mumbai MMR",
                Address = "Warehouse Complex 4, Sector B, Bhiwandi, Thane, Maharashtra 421302",
                IsPrimary = true,
                IsActive = true
            };
            var wh2 = new Warehouse
            {
                Id = Guid.Parse("b0000002-0000-0000-0000-000000000002"),
                Code = "WH-SOUTH",
                Name = "Southern Distribution Center",
                Location = "Nelamangala, Bengaluru",
                Address = "Plot 7A, Logistics Park, Nelamangala, Bengaluru 562123",
                IsPrimary = false,
                IsActive = true
            };

            await context.Warehouses.AddRangeAsync(wh1, wh2);
            await context.SaveChangesAsync();
        }

        // 5. Product Categories
        if (!await context.ProductCategories.AnyAsync())
        {
            var catGen = new ProductCategory
            {
                Id = Guid.Parse("ca000011-0000-0000-0000-000000000001"),
                Name = "General Merchandise",
                Code = "CAT-GEN",
                Description = "Standard consumer and commercial products",
                IsActive = true
            };
            var catElec = new ProductCategory
            {
                Id = Guid.Parse("ca000012-0000-0000-0000-000000000002"),
                Name = "Electronics & Sensors",
                Code = "CAT-ELEC",
                Description = "Electronic components, IoT sensors, and scanners",
                IsActive = true
            };
            var catOff = new ProductCategory
            {
                Id = Guid.Parse("ca000013-0000-0000-0000-000000000003"),
                Name = "Office & Packaging",
                Code = "CAT-OFF",
                Description = "Packaging materials, tapes, and supplies",
                IsActive = true
            };
            var catFurn = new ProductCategory
            {
                Id = Guid.Parse("ca000014-0000-0000-0000-000000000004"),
                Name = "Industrial Furniture",
                Code = "CAT-FURN",
                Description = "Ergonomic chairs and industrial work desks",
                IsActive = true
            };

            await context.ProductCategories.AddRangeAsync(catGen, catElec, catOff, catFurn);
            await context.SaveChangesAsync();
        }

        // 6. Products & Inventory
        if (!await context.Products.AnyAsync())
        {
            var catGenId = Guid.Parse("ca000011-0000-0000-0000-000000000001");
            var catElecId = Guid.Parse("ca000012-0000-0000-0000-000000000002");
            var catOffId = Guid.Parse("ca000013-0000-0000-0000-000000000003");
            var catFurnId = Guid.Parse("ca000014-0000-0000-0000-000000000004");
            var mainWarehouseId = Guid.Parse("b0000001-0000-0000-0000-000000000001");

            var products = new List<(Product prod, int initialStock)>
            {
                (new Product
                {
                    Id = Guid.Parse("fa000001-0000-0000-0000-000000000001"),
                    CategoryId = catGenId,
                    Sku = "PROD-A-001",
                    Name = "Product A",
                    Description = "Standard high-demand commercial grade Product A",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 60.00m,
                    SellingPrice = 100.00m, // Price ₹100 as in prompt scenario!
                    ReorderLevel = 15,
                    ReorderQuantity = 50,
                    IsActive = true
                }, 50), // Initial stock 50 as in prompt scenario!

                (new Product
                {
                    Id = Guid.Parse("fa000002-0000-0000-0000-000000000001"),
                    CategoryId = catGenId,
                    Sku = "PROD-B-002",
                    Name = "Product B",
                    Description = "Premium durable multi-purpose Product B",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 150.00m,
                    SellingPrice = 250.00m,
                    ReorderLevel = 20,
                    ReorderQuantity = 100,
                    IsActive = true
                }, 120),

                (new Product
                {
                    Id = Guid.Parse("fa000003-0000-0000-0000-000000000002"),
                    CategoryId = catElecId,
                    Sku = "IOT-SENS-101",
                    Name = "Smart IoT Sensor",
                    Description = "Wireless temperature and humidity telemetry sensor",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 320.00m,
                    SellingPrice = 499.00m,
                    ReorderLevel = 20,
                    ReorderQuantity = 50,
                    IsActive = true
                }, 85),

                (new Product
                {
                    Id = Guid.Parse("fa000004-0000-0000-0000-000000000002"),
                    CategoryId = catElecId,
                    Sku = "SCAN-WLS-202",
                    Name = "Wireless Barcode Scanner",
                    Description = "Handheld 2D QR and barcode Bluetooth scanner",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 1100.00m,
                    SellingPrice = 1850.00m,
                    ReorderLevel = 10,
                    ReorderQuantity = 25,
                    IsActive = true
                }, 30),

                (new Product
                {
                    Id = Guid.Parse("fa000005-0000-0000-0000-000000000003"),
                    CategoryId = catOffId,
                    Sku = "TAPE-HD-303",
                    Name = "Heavy Duty Packing Tape",
                    Description = "Brown sealing tape 65m industrial strength",
                    UnitOfMeasurement = "Roll",
                    CostPrice = 25.00m,
                    SellingPrice = 45.00m,
                    ReorderLevel = 50,
                    ReorderQuantity = 200,
                    IsActive = true
                }, 350),

                (new Product
                {
                    Id = Guid.Parse("fa000006-0000-0000-0000-000000000004"),
                    CategoryId = catFurnId,
                    Sku = "FURN-CHR-404",
                    Name = "Ergonomic Office Chair",
                    Description = "High-back mesh breathable lumbar support chair",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 2800.00m,
                    SellingPrice = 4500.00m,
                    ReorderLevel = 10,
                    ReorderQuantity = 15,
                    IsActive = true
                }, 8), // Low stock alert!

                (new Product
                {
                    Id = Guid.Parse("fa000007-0000-0000-0000-000000000002"),
                    CategoryId = catElecId,
                    Sku = "ELEC-MCU-505",
                    Name = "Microcontroller Board v3",
                    Description = "Embedded development board with Wi-Fi and BLE",
                    UnitOfMeasurement = "Unit",
                    CostPrice = 450.00m,
                    SellingPrice = 750.00m,
                    ReorderLevel = 15,
                    ReorderQuantity = 30,
                    IsActive = true
                }, 0) // Out of stock!
            };

            foreach (var item in products)
            {
                await context.Products.AddAsync(item.prod);
                var inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    ProductId = item.prod.Id,
                    WarehouseId = mainWarehouseId,
                    QuantityOnHand = item.initialStock,
                    ReservedQuantity = 0,
                    DamagedQuantity = 0,
                    MinimumStock = item.prod.ReorderLevel / 2
                };
                await context.Inventories.AddAsync(inv);

                if (item.initialStock > 0)
                {
                    var trans = new InventoryTransaction
                    {
                        Id = Guid.NewGuid(),
                        ProductId = item.prod.Id,
                        WarehouseId = mainWarehouseId,
                        TransactionType = InventoryTransactionType.Receipt,
                        QuantityChange = item.initialStock,
                        BalanceOnHandAfter = item.initialStock,
                        BalanceReservedAfter = 0,
                        ReferenceType = "OpeningStock",
                        ReferenceId = "INIT-2026",
                        Notes = "Initial opening inventory intake",
                        CreatedBy = "System"
                    };
                    await context.InventoryTransactions.AddAsync(trans);
                }
            }

            await context.SaveChangesAsync();
        }

        // 7. Suppliers
        if (!await context.Suppliers.AnyAsync())
        {
            var supp1 = new Supplier
            {
                Id = Guid.Parse("ea000001-0000-0000-0000-000000000001"),
                Code = "SUPP-TECH-01",
                Name = "TechLogistics Corp",
                ContactPerson = "Rajesh Sharma",
                Email = "orders@techlogistics.in",
                Phone = "+91 22 2847 1100",
                Address = "Tech Park, Whitefield, Bengaluru",
                PaymentTerms = "Net 30 Days",
                IsActive = true
            };

            var supp2 = new Supplier
            {
                Id = Guid.Parse("ea000002-0000-0000-0000-000000000002"),
                Code = "SUPP-GLOB-02",
                Name = "Global Electronics Supply",
                ContactPerson = "Priya Menon",
                Email = "sales@globalelec.com",
                Phone = "+91 44 6655 4433",
                Address = "SIPCOT Industrial Estate, Chennai",
                PaymentTerms = "Net 15 Days",
                IsActive = true
            };

            await context.Suppliers.AddRangeAsync(supp1, supp2);
            await context.SaveChangesAsync();
        }

        // 8. Application Settings
        if (!await context.ApplicationSettings.AnyAsync())
        {
            var settings = new List<ApplicationSetting>
            {
                new() { SettingKey = "tax_rate", SettingValue = "0.05", Description = "Default GST/Tax rate (5%)", Category = "Pricing" },
                new() { SettingKey = "currency", SettingValue = "INR", Description = "System base currency code", Category = "Localization" },
                new() { SettingKey = "currency_symbol", SettingValue = "₹", Description = "Currency visual display symbol", Category = "Localization" },
                new() { SettingKey = "shipping_flat_rate", SettingValue = "50.00", Description = "Standard flat rate shipping fee", Category = "Pricing" },
                new() { SettingKey = "shipping_free_threshold", SettingValue = "1000.00", Description = "Subtotal threshold for free shipping", Category = "Pricing" },
                new() { SettingKey = "company_name", SettingValue = "Apex Inventory & AI Commerce", Description = "Company business entity name", Category = "General" },
                new() { SettingKey = "support_email", SettingValue = "support@inventorysystem.com", Description = "Customer support email contact", Category = "General" },
                new() { SettingKey = "order_id_prefix", SettingValue = "ORD-", Description = "Prefix for generated sales order numbers", Category = "Orders" },
                new() { SettingKey = "low_stock_threshold_default", SettingValue = "10", Description = "Default threshold for low inventory notifications", Category = "Inventory" }
            };

            await context.ApplicationSettings.AddRangeAsync(settings);
            await context.SaveChangesAsync();
        }
    }
}
