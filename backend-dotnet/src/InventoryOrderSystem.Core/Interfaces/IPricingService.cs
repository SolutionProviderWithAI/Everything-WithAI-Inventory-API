using InventoryOrderSystem.Core.DTOs;

namespace InventoryOrderSystem.Core.Interfaces;

public interface IPricingService
{
    Task<OrderCalculationResult> CalculateOrderTotalsAsync(OrderCalculationRequest request, CancellationToken ct = default);
    Task<decimal> GetCurrentTaxRateAsync(CancellationToken ct = default);
    Task<decimal> CalculateShippingChargesAsync(decimal subtotal, CancellationToken ct = default);
}
