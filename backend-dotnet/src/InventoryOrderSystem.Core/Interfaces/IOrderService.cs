using InventoryOrderSystem.Core.DTOs;

namespace InventoryOrderSystem.Core.Interfaces;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderRequest request, string userId = "System", CancellationToken ct = default);
    Task<OrderDto> ConfirmOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default);
    Task<OrderDto> DispatchOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default);
    Task<OrderDto> DeliverOrderAsync(Guid orderId, string userId = "System", CancellationToken ct = default);
    Task<OrderDto> CancelOrderAsync(Guid orderId, string reason, string userId = "System", CancellationToken ct = default);
    Task<OrderDto> GetOrderByIdAsync(Guid id, CancellationToken ct = default);
    Task<OrderDto> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<PagedResult<OrderDto>> GetOrdersAsync(Guid? customerId, string? status, DateTime? fromDate, DateTime? toDate, int page, int pageSize, CancellationToken ct = default);
}
