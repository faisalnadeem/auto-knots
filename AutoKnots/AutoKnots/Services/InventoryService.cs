using AutoKnots.Data;
using AutoKnots.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Services;

    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _db;

        public InventoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<InventoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _db.InventoryItems
                .Include(x => x.Investments)
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<InventoryListResult> GetListAsync(string? search, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.InventoryItems.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(currentUserId))
            {
                query = query.Where(x =>
                    x.CreatedByUserId == currentUserId ||
                    x.Investments.Any(inv => inv.InvestorUserId == currentUserId));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x =>
                    (x.Name != null && x.Name.Contains(term)) ||
                    (x.Make != null && x.Make.Contains(term)) ||
                    (x.Model != null && x.Model.Contains(term)) ||
                    (x.Variant != null && x.Variant.Contains(term)));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var items = await query
                .OrderBy(x => x.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new InventoryListResult
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

    public async Task<InventoryServiceResult> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        item.Id = 0;
        item.CreatedAt = DateTime.UtcNow;

        // New items start in Draft unless the caller explicitly sets a status.
        if (item.Status == default)
        {
            item.Status = InventoryStatus.Draft;
        }

        // Ensure IsActive reflects the workflow status.
        item.IsActive = item.Status == InventoryStatus.Active;

        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        return new InventoryServiceResult { Success = true, Item = item };
    }

    public async Task<InventoryServiceResult> UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        var existing = await _db.InventoryItems.FindAsync(new object[] { item.Id }, cancellationToken);
        if (existing == null)
            return new InventoryServiceResult { Success = false, Error = "Inventory item not found." };

        existing.Name = item.Name;
        existing.Make = item.Make;
        existing.Model = item.Model;
        existing.Variant = item.Variant;
        existing.PurchaseDate = item.PurchaseDate;
        existing.EngineNumber = item.EngineNumber;
        existing.ChassisNumber = item.ChassisNumber;
        existing.CostPrice = item.CostPrice;
        existing.SalePrice = item.SalePrice;
        existing.MinimumStock = item.MinimumStock;

        // Only change status when caller explicitly sets it; otherwise keep existing.
        var newStatus = item.Status == default ? existing.Status : item.Status;
        existing.Status = newStatus;
        existing.IsActive = newStatus == InventoryStatus.Active;

        await _db.SaveChangesAsync(cancellationToken);
        return new InventoryServiceResult { Success = true, Item = existing };
    }

    public async Task<InventoryServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _db.InventoryItems.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
            return new InventoryServiceResult { Success = false, Error = "Inventory item not found." };
        _db.InventoryItems.Remove(existing);
        await _db.SaveChangesAsync(cancellationToken);
        return new InventoryServiceResult { Success = true };
    }

}
