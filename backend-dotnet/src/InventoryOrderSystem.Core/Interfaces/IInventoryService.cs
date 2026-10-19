using InventoryOrderSystem.Core.DTOs;

namespace InventoryOrderSystem.Core.Interfaces;

public interface IInventoryService
{
    Task<int> GetAvailableToSellAsync(Guid productId, Guid? warehouseId = null, CancellationToken ct = default);
    Task<List<InventoryDto>> GetProductStockAsync(Guid productId, CancellationToken ct = default);
    Task<List<LowStockDto>> GetLowStockAlertsAsync(CancellationToken ct = default);
    Task<PagedResult<InventoryMovementDto>> GetStockMovementsAsync(Guid? productId, Guid? warehouseId, int page, int pageSize, CancellationToken ct = default);
    Task<bool> ReserveStockAsync(Guid salesOrderId, Guid productId, int quantity, Guid? warehouseId = null, CancellationToken ct = default);
    Task ReleaseReservationAsync(Guid salesOrderId, string reason = "Order cancelled", string userId = "System", CancellationToken ct = default);
    Task FulfillReservationAndDeductStockAsync(Guid salesOrderId, string userId = "System", CancellationToken ct = default);
    Task<InventoryDto> AdjustStockAsync(StockAdjustmentRequest request, string userId = "System", CancellationToken ct = default);
    Task<InventoryDto> ReceiveStockAsync(StockReceiptRequest request, string userId = "System", CancellationToken ct = default);
}
