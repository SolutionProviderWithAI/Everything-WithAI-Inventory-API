using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using InventoryOrderSystem.Core.DTOs;
using InventoryOrderSystem.Core.Interfaces;

namespace InventoryOrderSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPricingService _pricingService;

    public OrdersController(IOrderService orderService, IPricingService pricingService)
    {
        _orderService = orderService;
        _pricingService = pricingService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] Guid? customerId,
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var orders = await _orderService.GetOrdersAsync(customerId, status, fromDate, toDate, page, pageSize, ct);
        return Ok(orders);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByIdAsync(id, ct);
        return Ok(order);
    }

    [HttpGet("number/{orderNumber}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber, CancellationToken ct)
    {
        var order = await _orderService.GetOrderByNumberAsync(orderNumber, ct);
        return Ok(order);
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(OrderCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CalculateOrderTotals([FromBody] OrderCalculationRequest request, CancellationToken ct)
    {
        var result = await _pricingService.CalculateOrderTotalsAsync(request, ct);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Customer";
        var order = await _orderService.CreateOrderAsync(request, userId, ct);
        return CreatedAtAction(nameof(GetOrderById), new { id = order.Id }, order);
    }

    [HttpPost("{id}/confirm")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmOrder(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Customer";
        var order = await _orderService.ConfirmOrderAsync(id, userId, ct);
        return Ok(order);
    }

    [HttpPost("{id}/dispatch")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DispatchOrder(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Staff";
        var order = await _orderService.DispatchOrderAsync(id, userId, ct);
        return Ok(order);
    }

    [HttpPost("{id}/deliver")]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeliverOrder(Guid id, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Staff";
        var order = await _orderService.DeliverOrderAsync(id, userId, ct);
        return Ok(order);
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest? request, CancellationToken ct)
    {
        var userId = User.Identity?.Name ?? "Customer";
        var reason = request?.Reason ?? "Customer requested cancellation";
        var order = await _orderService.CancelOrderAsync(id, reason, userId, ct);
        return Ok(order);
    }
}
