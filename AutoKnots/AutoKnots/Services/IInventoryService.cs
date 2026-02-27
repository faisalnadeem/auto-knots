using AutoKnots.Models;

namespace AutoKnots.Services;

public interface IInventoryService
{
    Task<InventoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InventoryListResult> GetListAsync(string? search, int page, int pageSize, string? currentUserId = null, CancellationToken cancellationToken = default);
    Task<InventoryServiceResult> CreateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<InventoryServiceResult> UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task<InventoryServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class InventoryListResult
{
    public List<InventoryItem> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class InventoryServiceResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public InventoryItem? Item { get; set; }
}
