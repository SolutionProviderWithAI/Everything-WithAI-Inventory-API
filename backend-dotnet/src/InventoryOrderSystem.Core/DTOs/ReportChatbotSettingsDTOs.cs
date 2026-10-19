namespace InventoryOrderSystem.Core.DTOs;

public class SalesReportDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal AverageOrderValue => TotalOrders > 0 ? Math.Round(TotalRevenue / TotalOrders, 2) : 0m;
    public List<DailySalesDataDto> DailySales { get; set; } = new();
    public List<BestSellerDto> TopProducts { get; set; } = new();
    public List<OrdersByStatusDto> OrdersByStatus { get; set; } = new();
}

public class DailySalesDataDto
{
    public string Date { get; set; } = string.Empty;
    public int OrderCount { get; set; }
    public decimal TotalSales { get; set; }
}

public class BestSellerDto
{
    public Guid ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal RevenueGenerated { get; set; }
}

public class OrdersByStatusDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal TotalAmount { get; set; }
}

public class InventoryValuationDto
{
    public int TotalActiveProducts { get; set; }
    public int TotalUnitsOnHand { get; set; }
    public int TotalUnitsReserved { get; set; }
    public int TotalUnitsAvailable { get; set; }
    public decimal TotalCostValuation { get; set; }
    public decimal TotalRetailValuation { get; set; }
    public int LowStockProductCount { get; set; }
    public int OutOfStockProductCount { get; set; }
    public List<WarehouseValuationDto> WarehouseBreakdown { get; set; } = new();
}

public class WarehouseValuationDto
{
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalUnits { get; set; }
    public decimal TotalCostValue { get; set; }
}

public class ChatbotConversationDto
{
    public Guid Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Channel { get; set; } = "WebChat";
    public DateTime StartedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}

public class ChatbotMessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public string? IntentDetected { get; set; }
    public string? StructuredData { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ChatbotLogRequest
{
    public string SessionId { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string Sender { get; set; } = "Customer";
    public string MessageText { get; set; } = string.Empty;
    public string? IntentDetected { get; set; }
    public string? StructuredData { get; set; }
}

public class SettingDto
{
    public Guid Id { get; set; }
    public string SettingKey { get; set; } = string.Empty;
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "General";
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSettingDto
{
    public string SettingValue { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserEmail { get; set; }
    public DateTime Timestamp { get; set; }
}
