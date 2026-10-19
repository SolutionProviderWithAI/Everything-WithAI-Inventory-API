using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Interfaces;

namespace InventoryOrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet("{productId}")]
    [ProducesResponseType(typeof(List<InventoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductStock(Guid productId, CancellationToken ct)
    {
        var stock = await _inventoryService.GetProductStockAsync(productId, ct);
        return Ok(stock);
    }

    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<LowStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStockAlerts(CancellationToken ct)
    {
        var alerts = await _inventoryService.GetLowStockAlertsAsync(ct);
        return Ok(alerts);
    }

    [HttpGet("movements")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(PagedResult<InventoryMovementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockMovements(
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var movements = await _inventoryService.GetStockMovementsAsync(productId, warehouseId, page, pageSize, ct);
        return Ok(movements);
    }

    [HttpPost("adjustments")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Staff";
        var result = await _inventoryService.AdjustStockAsync(request, userId, ct);
        return Ok(result);
    }

    [HttpPost("receipts")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReceiveStock([FromBody] StockReceiptRequest request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Staff";
        var result = await _inventoryService.ReceiveStockAsync(request, userId, ct);
        return Ok(result);
    }
}
