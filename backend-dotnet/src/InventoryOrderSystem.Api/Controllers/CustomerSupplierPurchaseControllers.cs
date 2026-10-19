using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Entities;
using InventoryOrderSystem.Core.Enums;
using InventoryOrderSystem.Core.Exceptions;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;

namespace InventoryOrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(List<CustomerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers(CancellationToken ct)
    {
        var customers = await _context.Customers
            .Include(c => c.Addresses)
            .Include(c => c.Orders)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                CustomerCode = c.CustomerCode,
                Name = c.Name,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                TaxNumber = c.TaxNumber,
                CreditLimit = c.CreditLimit,
                OutstandingBalance = c.OutstandingBalance,
                IsActive = c.IsActive,
                OrderCount = c.Orders.Count,
                Addresses = c.Addresses.Select(a => new CustomerAddressDto
                {
                    Id = a.Id,
                    AddressType = a.AddressType.ToString(),
                    Line1 = a.Line1,
                    Line2 = a.Line2,
                    City = a.City,
                    State = a.State,
                    PostalCode = a.PostalCode,
                    Country = a.Country,
                    IsDefault = a.IsDefault
                }).ToList()
            })
            .ToListAsync(ct);

        return Ok(customers);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerById(Guid id, CancellationToken ct)
    {
        var c = await _context.Customers
            .Include(c => c.Addresses)
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        var dto = new CustomerDto
        {
            Id = c.Id,
            CustomerCode = c.CustomerCode,
            Name = c.Name,
            Email = c.Email,
            Phone = c.Phone,
            CompanyName = c.CompanyName,
            TaxNumber = c.TaxNumber,
            CreditLimit = c.CreditLimit,
            OutstandingBalance = c.OutstandingBalance,
            IsActive = c.IsActive,
            OrderCount = c.Orders.Count,
            Addresses = c.Addresses.Select(a => new CustomerAddressDto
            {
                Id = a.Id,
                AddressType = a.AddressType.ToString(),
                Line1 = a.Line1,
                Line2 = a.Line2,
                City = a.City,
                State = a.State,
                PostalCode = a.PostalCode,
                Country = a.Country,
                IsDefault = a.IsDefault
            }).ToList()
        };

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(CustomerDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, CancellationToken ct)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CustomerCode = $"CUST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            Name = dto.Name.Trim(),
            Email = dto.Email.Trim(),
            Phone = dto.Phone.Trim(),
            CompanyName = dto.CompanyName,
            TaxNumber = dto.TaxNumber,
            CreditLimit = dto.CreditLimit,
            OutstandingBalance = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(dto.AddressLine1))
        {
            customer.Addresses.Add(new CustomerAddress
            {
                Id = Guid.NewGuid(),
                CustomerId = customer.Id,
                AddressType = AddressType.Both,
                Line1 = dto.AddressLine1,
                City = dto.City ?? "Mumbai",
                State = dto.State ?? "Maharashtra",
                PostalCode = dto.PostalCode ?? "400001",
                Country = "India",
                IsDefault = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.Customers.AddAsync(customer, ct);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetCustomerById), new { id = customer.Id }, new CustomerDto
        {
            Id = customer.Id,
            CustomerCode = customer.CustomerCode,
            Name = customer.Name,
            Email = customer.Email,
            Phone = customer.Phone,
            CompanyName = customer.CompanyName,
            TaxNumber = customer.TaxNumber,
            CreditLimit = customer.CreditLimit,
            OutstandingBalance = customer.OutstandingBalance,
            IsActive = customer.IsActive,
            OrderCount = 0
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(List<SupplierDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuppliers(CancellationToken ct)
    {
        var suppliers = await _context.Suppliers
            .Include(s => s.SupplierProducts)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SupplierDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                ContactPerson = s.ContactPerson,
                Email = s.Email,
                Phone = s.Phone,
                Address = s.Address,
                PaymentTerms = s.PaymentTerms,
                IsActive = s.IsActive,
                ProductCount = s.SupplierProducts.Count
            })
            .ToListAsync(ct);

        return Ok(suppliers);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(SupplierDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto, CancellationToken ct)
    {
        if (await _context.Suppliers.AnyAsync(s => s.Code.ToLower() == dto.Code.Trim().ToLower(), ct))
        {
            throw new BusinessRuleValidationException($"Supplier with code '{dto.Code}' already exists.");
        }

        var supplier = new Supplier
        {
            Id = Guid.NewGuid(),
            Code = dto.Code.Trim().ToUpper(),
            Name = dto.Name.Trim(),
            ContactPerson = dto.ContactPerson,
            Email = dto.Email.Trim(),
            Phone = dto.Phone,
            Address = dto.Address,
            PaymentTerms = dto.PaymentTerms ?? "Net 30 Days",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Suppliers.AddAsync(supplier, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(new SupplierDto
        {
            Id = supplier.Id,
            Code = supplier.Code,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            PaymentTerms = supplier.PaymentTerms,
            IsActive = supplier.IsActive,
            ProductCount = 0
        });
    }
}

[ApiController]
[Route("api/[controller]")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IInventoryService _inventoryService;

    public PurchaseOrdersController(AppDbContext context, IInventoryService inventoryService)
    {
        _context = context;
        _inventoryService = inventoryService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(List<PurchaseOrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrders(CancellationToken ct)
    {
        var pos = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Warehouse)
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .OrderByDescending(p => p.OrderDate)
            .Select(p => new PurchaseOrderDto
            {
                Id = p.Id,
                OrderNumber = p.OrderNumber,
                SupplierId = p.SupplierId,
                SupplierName = p.Supplier.Name,
                WarehouseId = p.WarehouseId,
                WarehouseName = p.Warehouse.Name,
                OrderDate = p.OrderDate,
                ExpectedDeliveryDate = p.ExpectedDeliveryDate,
                Status = p.Status.ToString(),
                TotalAmount = p.TotalAmount,
                CreatedBy = p.CreatedBy,
                Notes = p.Notes,
                Items = p.Items.Select(i => new PurchaseOrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductSku = i.Product.Sku,
                    ProductName = i.Product.Name,
                    QuantityOrdered = i.QuantityOrdered,
                    QuantityReceived = i.QuantityReceived,
                    UnitCost = i.UnitCost,
                    LineTotal = i.LineTotal
                }).ToList()
            })
            .ToListAsync(ct);

        return Ok(pos);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PurchaseOrderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePurchaseOrder([FromBody] CreatePurchaseOrderDto dto, CancellationToken ct)
    {
        var supplier = await _context.Suppliers.FindAsync(new object[] { dto.SupplierId }, ct)
            ?? throw new NotFoundException(nameof(Supplier), dto.SupplierId);

        var warehouse = await _context.Warehouses.FindAsync(new object[] { dto.WarehouseId }, ct)
            ?? throw new NotFoundException(nameof(Warehouse), dto.WarehouseId);

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = $"PO-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            SupplierId = dto.SupplierId,
            WarehouseId = dto.WarehouseId,
            OrderDate = DateTime.UtcNow,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate ?? DateTime.UtcNow.AddDays(7),
            Status = PurchaseOrderStatus.Submitted,
            CreatedBy = User.Identity?.Name ?? "Admin",
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        decimal total = 0m;
        foreach (var item in dto.Items)
        {
            var product = await _context.Products.FindAsync(new object[] { item.ProductId }, ct)
                ?? throw new NotFoundException(nameof(Product), item.ProductId);

            var lineTotal = item.Quantity * item.UnitCost;
            total += lineTotal;

            po.Items.Add(new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                ProductId = item.ProductId,
                QuantityOrdered = item.Quantity,
                QuantityReceived = 0,
                UnitCost = item.UnitCost,
                LineTotal = lineTotal
            });
        }

        po.TotalAmount = total;
        await _context.PurchaseOrders.AddAsync(po, ct);
        await _context.SaveChangesAsync(ct);

        return Ok(po);
    }

    [HttpPost("{id}/receive")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveItems(Guid id, [FromBody] ReceivePurchaseOrderDto dto, CancellationToken ct)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new NotFoundException(nameof(PurchaseOrder), id);

        var userId = User.Identity?.Name ?? "Staff";

        foreach (var itemDto in dto.Items)
        {
            var item = po.Items.FirstOrDefault(i => i.Id == itemDto.PurchaseOrderItemId);
            if (item != null && itemDto.QuantityToReceive > 0)
            {
                var remaining = item.QuantityOrdered - item.QuantityReceived;
                var toReceive = Math.Min(remaining, itemDto.QuantityToReceive);
                item.QuantityReceived += toReceive;

                await _inventoryService.ReceiveStockAsync(new StockReceiptRequest
                {
                    ProductId = item.ProductId,
                    WarehouseId = po.WarehouseId,
                    QuantityReceived = toReceive,
                    ReferenceType = "PurchaseOrder",
                    ReferenceId = po.OrderNumber,
                    Notes = $"Received on PO {po.OrderNumber}"
                }, userId, ct);
            }
        }

        // Update PO status
        bool allReceived = po.Items.All(i => i.QuantityReceived >= i.QuantityOrdered);
        po.Status = allReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        po.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);
        return Ok(new { message = "Stock received successfully", status = po.Status.ToString() });
    }
}
