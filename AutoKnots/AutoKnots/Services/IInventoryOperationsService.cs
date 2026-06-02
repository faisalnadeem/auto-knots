using AutoKnots.Models;
using AutoKnots.Models.Api;

namespace AutoKnots.Services;

public interface IInventoryOperationsService
{
    Task<InventoryServiceResult> CreateWithInvestorsAsync(InventoryItem item, IList<InvestmentAllocationRequest>? investors, CancellationToken cancellationToken = default);
    Task<InventoryServiceResult> UpdateWithInvestorsAsync(InventoryItem item, IList<InvestmentAllocationRequest>? investors, CancellationToken cancellationToken = default);
    Task<IList<InventoryCost>> GetCostsAsync(int inventoryItemId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, InventoryCost? Cost)> AddCostAsync(int inventoryItemId, AddCostRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, InventoryCost? Cost)> UpdateCostAsync(int costId, UpdateCostRequest request, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SellVehicleAsync(int inventoryItemId, decimal sellingPrice, CancellationToken cancellationToken = default);
    Task<InventoryProfitViewModel?> GetProfitDetailAsync(int inventoryItemId, CancellationToken cancellationToken = default);
}
