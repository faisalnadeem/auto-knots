using AutoKnots.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Services;

public class InventoryAuthorizationService : IInventoryAuthorizationService
{
    private readonly ApplicationDbContext _db;

    public InventoryAuthorizationService(ApplicationDbContext db) => _db = db;

    public Task<bool> CanViewAsync(int inventoryItemId, string userId, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AnyAsync(item =>
            item.Id == inventoryItemId &&
            (item.CreatedByUserId == userId ||
             item.Investments.Any(investment => investment.InvestorUserId == userId)),
            cancellationToken);

    public Task<bool> CanManageAsync(int inventoryItemId, string userId, CancellationToken cancellationToken = default) =>
        _db.InventoryItems.AnyAsync(item =>
            item.Id == inventoryItemId && item.CreatedByUserId == userId,
            cancellationToken);
}
