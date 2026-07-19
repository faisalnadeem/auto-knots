namespace AutoKnots.Services;

public interface IInventoryAuthorizationService
{
    Task<bool> CanViewAsync(int inventoryItemId, string userId, CancellationToken cancellationToken = default);
    Task<bool> CanManageAsync(int inventoryItemId, string userId, CancellationToken cancellationToken = default);
}
