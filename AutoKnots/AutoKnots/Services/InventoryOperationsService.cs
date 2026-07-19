using AutoKnots.Data;
using AutoKnots.Models;
using AutoKnots.Models.Api;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Services;

public class InventoryOperationsService : IInventoryOperationsService
{
    private readonly ApplicationDbContext _db;
    private readonly IInventoryService _inventoryService;

    public InventoryOperationsService(ApplicationDbContext db, IInventoryService inventoryService)
    {
        _db = db;
        _inventoryService = inventoryService;
    }

    public async Task<InventoryServiceResult> CreateWithInvestorsAsync(
        InventoryItem item,
        IList<InvestmentAllocationRequest>? investors,
        CancellationToken cancellationToken = default)
    {
        var investorList = investors?.Where(i => !string.IsNullOrWhiteSpace(i.InvestorUserId)).ToList() ?? new List<InvestmentAllocationRequest>();

        if (investorList.Count > 0)
        {
            var validationError = ValidateInvestorAllocations(investorList, item.CostPrice);
            if (validationError != null)
                return new InventoryServiceResult { Success = false, Error = validationError };

            item.Status = InventoryStatus.PendingApproval;
            item.IsActive = false;
        }
        else
        {
            item.Status = InventoryStatus.Active;
            item.IsActive = true;
        }

        var result = await _inventoryService.CreateAsync(item, cancellationToken);
        if (!result.Success || result.Item == null)
            return result;

        await ApplyInvestmentsAndInitialCostsAsync(result.Item, investorList, cancellationToken);
        return result;
    }

    public async Task<InventoryServiceResult> UpdateWithInvestorsAsync(
        InventoryItem item,
        IList<InvestmentAllocationRequest>? investors,
        CancellationToken cancellationToken = default)
    {
        var investorList = investors?.Where(i => !string.IsNullOrWhiteSpace(i.InvestorUserId)).ToList() ?? new List<InvestmentAllocationRequest>();

        if (investorList.Count > 0)
        {
            var validationError = ValidateInvestorAllocations(investorList, item.CostPrice);
            if (validationError != null)
                return new InventoryServiceResult { Success = false, Error = validationError };

            item.Status = InventoryStatus.PendingApproval;
        }

        var result = await _inventoryService.UpdateAsync(item, cancellationToken);
        if (!result.Success || result.Item == null)
            return result;

        if (investorList.Count > 0)
        {
            var existingInvestments = await _db.InventoryInvestments
                .Where(x => x.InventoryItemId == result.Item.Id)
                .ToListAsync(cancellationToken);

            var totalCost = result.Item.CostPrice;

            foreach (var allocation in investorList)
            {
                var (amount, percentage) = ResolveAllocation(allocation, totalCost);
                if (amount <= 0 && (percentage == null || percentage <= 0))
                    continue;

                var existing = existingInvestments.FirstOrDefault(x => x.InvestorUserId == allocation.InvestorUserId);
                if (existing != null)
                {
                    existing.Amount = amount;
                    existing.Percentage = percentage;
                }
                else
                {
                    _db.InventoryInvestments.Add(new InventoryInvestment
                    {
                        InventoryItemId = result.Item.Id,
                        InvestorUserId = allocation.InvestorUserId,
                        Amount = amount,
                        Percentage = percentage,
                        Status = InvestmentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<IList<InventoryCost>> GetCostsAsync(int inventoryItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

        if (item == null)
            return Array.Empty<InventoryCost>();

        var costs = await _db.InventoryCosts
            .Where(c => c.InventoryItemId == inventoryItemId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        if (costs.Count == 0)
        {
            costs = await BackfillInitialCostsAsync(item, cancellationToken);
        }

        return costs;
    }

    public async Task<(bool Success, string? Error, InventoryCost? Cost)> AddCostAsync(
        int inventoryItemId,
        AddCostRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

        if (item == null)
            return (false, "Inventory item not found.", null);

        if (item.Status == InventoryStatus.Sold)
            return (false, "This vehicle has been sold. You cannot add new costs.", null);

        var investment = item.Investments.FirstOrDefault(x => x.InvestorUserId == request.InvestorUserId);
        var isCreator = !string.IsNullOrEmpty(item.CreatedByUserId) && item.CreatedByUserId == request.InvestorUserId;

        if (investment == null && !isCreator)
            return (false, "Selected investor is not associated with this vehicle.", null);

        var cost = new InventoryCost
        {
            InventoryItemId = item.Id,
            InvestorUserId = request.InvestorUserId,
            Amount = request.Amount,
            Type = request.Type,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryCosts.Add(cost);
        item.CostPrice += request.Amount;

        if (investment != null)
            investment.Amount += request.Amount;

        RecalculateInvestmentPercentages(item);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, cost);
    }

    public async Task<(bool Success, string? Error, InventoryCost? Cost)> UpdateCostAsync(
        int inventoryItemId,
        int costId,
        UpdateCostRequest request,
        CancellationToken cancellationToken = default)
    {
        var cost = await _db.InventoryCosts.FirstOrDefaultAsync(
            c => c.Id == costId && c.InventoryItemId == inventoryItemId,
            cancellationToken);
        if (cost == null)
            return (false, "Cost entry not found.", null);

        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == cost.InventoryItemId, cancellationToken);

        if (item == null)
            return (false, "Inventory item not found.", null);

        if (item.Status == InventoryStatus.Sold)
            return (false, "This vehicle has been sold. Costs cannot be modified.", null);

        var previousAmount = cost.Amount;
        var previousInvestorId = cost.InvestorUserId;

        cost.InvestorUserId = request.InvestorUserId;
        cost.Amount = request.Amount;
        cost.Type = request.Type;
        cost.Notes = request.Notes;
        cost.UpdatedAt = DateTime.UtcNow;

        item.CostPrice += request.Amount - previousAmount;

        if (previousInvestorId != null)
        {
            var oldInvestment = item.Investments.FirstOrDefault(x => x.InvestorUserId == previousInvestorId);
            if (oldInvestment != null)
                oldInvestment.Amount -= previousAmount;
        }

        var newInvestment = item.Investments.FirstOrDefault(x => x.InvestorUserId == request.InvestorUserId);
        if (newInvestment != null)
            newInvestment.Amount += request.Amount;

        RecalculateInvestmentPercentages(item);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, null, cost);
    }

    public async Task<(bool Success, string? Error)> SellVehicleAsync(
        int inventoryItemId,
        decimal sellingPrice,
        CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

        if (item == null)
            return (false, "Inventory item not found.");

        if (item.Status == InventoryStatus.Sold)
            return (false, "This vehicle has already been marked as sold.");

        item.SalePrice = sellingPrice;
        var profit = sellingPrice - item.CostPrice;

        if (profit > 0 && item.Investments.Any())
        {
            var approvedInvestments = item.Investments
                .Where(x => x.Status == InvestmentStatus.Approved)
                .ToList();

            if (!approvedInvestments.Any())
                approvedInvestments = item.Investments.ToList();

            if (approvedInvestments.Any())
            {
                var totalPercentage = 0m;

                foreach (var inv in approvedInvestments)
                {
                    if (!inv.Percentage.HasValue || inv.Percentage.Value <= 0)
                    {
                        if (item.CostPrice > 0)
                            inv.Percentage = Math.Round((inv.Amount / item.CostPrice) * 100m, 2);
                    }

                    if (inv.Percentage.HasValue && inv.Percentage.Value > 0)
                        totalPercentage += inv.Percentage.Value;
                }

                var creatorId = item.CreatedByUserId;
                decimal creatorPercentage = 0m;
                if (!string.IsNullOrEmpty(creatorId) && item.CostPrice > 0)
                {
                    var totalInvestorAmount = approvedInvestments.Sum(x => x.Amount);
                    var creatorAmount = item.CostPrice - totalInvestorAmount;
                    if (creatorAmount > 0)
                    {
                        creatorPercentage = Math.Round((creatorAmount / item.CostPrice) * 100m, 2);
                        totalPercentage += creatorPercentage;
                    }
                }

                if (totalPercentage > 0)
                {
                    foreach (var inv in approvedInvestments)
                    {
                        var pct = inv.Percentage ?? 0m;
                        if (pct <= 0)
                            continue;

                        var share = Math.Round(profit * (pct / totalPercentage), 2);
                        if (share <= 0)
                            continue;

                        _db.InventoryCosts.Add(new InventoryCost
                        {
                            InventoryItemId = item.Id,
                            InvestorUserId = inv.InvestorUserId,
                            Amount = share,
                            Type = "Profit Share",
                            Notes = $"Profit distribution for sale at {sellingPrice:N2}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    if (creatorPercentage > 0 && !string.IsNullOrEmpty(item.CreatedByUserId))
                    {
                        var creatorShare = Math.Round(profit * (creatorPercentage / totalPercentage), 2);
                        if (creatorShare > 0)
                        {
                            _db.InventoryCosts.Add(new InventoryCost
                            {
                                InventoryItemId = item.Id,
                                InvestorUserId = item.CreatedByUserId,
                                Amount = creatorShare,
                                Type = "Profit Share",
                                Notes = $"Creator profit share for sale at {sellingPrice:N2}",
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }
        else if (profit > 0 && !string.IsNullOrEmpty(item.CreatedByUserId))
        {
            _db.InventoryCosts.Add(new InventoryCost
            {
                InventoryItemId = item.Id,
                InvestorUserId = item.CreatedByUserId,
                Amount = profit,
                Type = "Profit Share",
                Notes = $"Creator profit (no other investors) for sale at {sellingPrice:N2}",
                CreatedAt = DateTime.UtcNow
            });
        }

        item.Status = InventoryStatus.Sold;
        item.IsActive = false;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<InventoryProfitViewModel?> GetProfitDetailAsync(int inventoryItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == inventoryItemId, cancellationToken);

        if (item == null)
            return null;

        var profitCosts = await _db.InventoryCosts
            .Where(c => c.InventoryItemId == inventoryItemId && c.Type == "Profit Share")
            .ToListAsync(cancellationToken);

        var investorIds = item.Investments
            .Select(x => x.InvestorUserId)
            .Distinct()
            .ToList();

        if (!string.IsNullOrEmpty(item.CreatedByUserId) && !investorIds.Contains(item.CreatedByUserId))
            investorIds.Add(item.CreatedByUserId);

        var investors = await _db.Users
            .Where(u => investorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id, cancellationToken);

        var rows = new List<InventoryProfitRow>();
        foreach (var inv in item.Investments)
        {
            var totalProfitForInvestor = profitCosts
                .Where(c => c.InvestorUserId == inv.InvestorUserId)
                .Sum(c => c.Amount);

            investors.TryGetValue(inv.InvestorUserId, out var name);

            rows.Add(new InventoryProfitRow
            {
                InvestorUserId = inv.InvestorUserId,
                InvestorName = name ?? inv.InvestorUserId,
                Percentage = inv.Percentage ?? 0m,
                ProfitAmount = totalProfitForInvestor
            });
        }

        if (!string.IsNullOrEmpty(item.CreatedByUserId))
        {
            var totalInvestorAmount = item.Investments.Sum(x => x.Amount);
            var creatorAmount = item.CostPrice - totalInvestorAmount;
            decimal creatorPercentage = 0m;
            if (creatorAmount > 0 && item.CostPrice > 0)
                creatorPercentage = Math.Round((creatorAmount / item.CostPrice) * 100m, 2);

            var creatorProfit = profitCosts
                .Where(c => c.InvestorUserId == item.CreatedByUserId)
                .Sum(c => c.Amount);

            investors.TryGetValue(item.CreatedByUserId, out var creatorName);

            rows.Add(new InventoryProfitRow
            {
                InvestorUserId = item.CreatedByUserId,
                InvestorName = creatorName ?? item.CreatedByUserId,
                Percentage = creatorPercentage,
                ProfitAmount = creatorProfit
            });
        }

        return new InventoryProfitViewModel
        {
            InventoryItemId = item.Id,
            InventoryName = item.Name,
            TotalCost = item.CostPrice,
            SellingPrice = item.SalePrice,
            TotalProfit = item.SalePrice - item.CostPrice,
            Rows = rows
        };
    }

    private async Task ApplyInvestmentsAndInitialCostsAsync(
        InventoryItem item,
        IList<InvestmentAllocationRequest> investorList,
        CancellationToken cancellationToken)
    {
        var totalCost = item.CostPrice;
        var investments = new List<InventoryInvestment>();
        var costs = new List<InventoryCost>();
        decimal totalInvestorAmount = 0m;

        foreach (var allocation in investorList)
        {
            var (amount, percentage) = ResolveAllocation(allocation, totalCost);
            if (amount <= 0 && (percentage == null || percentage <= 0))
                continue;

            totalInvestorAmount += amount;

            investments.Add(new InventoryInvestment
            {
                InventoryItemId = item.Id,
                InvestorUserId = allocation.InvestorUserId,
                Amount = amount,
                Percentage = percentage,
                Status = InvestmentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            costs.Add(new InventoryCost
            {
                InventoryItemId = item.Id,
                InvestorUserId = allocation.InvestorUserId,
                Amount = amount,
                Type = "Initial Investment",
                CreatedAt = DateTime.UtcNow
            });
        }

        var creatorShare = totalCost - totalInvestorAmount;
        if (creatorShare > 0 && !string.IsNullOrEmpty(item.CreatedByUserId))
        {
            costs.Add(new InventoryCost
            {
                InventoryItemId = item.Id,
                InvestorUserId = item.CreatedByUserId,
                Amount = creatorShare,
                Type = "Initial Investment",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (investments.Count > 0)
            _db.InventoryInvestments.AddRange(investments);

        if (costs.Count > 0)
            _db.InventoryCosts.AddRange(costs);

        if (investments.Count > 0 || costs.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<InventoryCost>> BackfillInitialCostsAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        var newCosts = new List<InventoryCost>();
        var totalInvestorAmount = 0m;

        if (item.Investments != null && item.Investments.Any())
        {
            foreach (var inv in item.Investments)
            {
                totalInvestorAmount += inv.Amount;

                if (inv.Amount > 0 && !string.IsNullOrEmpty(inv.InvestorUserId))
                {
                    newCosts.Add(new InventoryCost
                    {
                        InventoryItemId = item.Id,
                        InvestorUserId = inv.InvestorUserId,
                        Amount = inv.Amount,
                        Type = "Initial Investment",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        var creatorShare = item.CostPrice - totalInvestorAmount;
        if (creatorShare > 0 && !string.IsNullOrEmpty(item.CreatedByUserId))
        {
            newCosts.Add(new InventoryCost
            {
                InventoryItemId = item.Id,
                InvestorUserId = item.CreatedByUserId,
                Amount = creatorShare,
                Type = "Initial Investment",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (newCosts.Count > 0)
        {
            _db.InventoryCosts.AddRange(newCosts);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return newCosts.OrderByDescending(c => c.CreatedAt).ToList();
    }

    private static string? ValidateInvestorAllocations(IList<InvestmentAllocationRequest> investors, decimal totalCost)
    {
        var hasValidAllocation = investors.Any(allocation =>
        {
            var (amount, percentage) = ResolveAllocation(allocation, totalCost);
            return amount > 0 || percentage is > 0;
        });

        return hasValidAllocation
            ? null
            : "You selected investors but did not enter any amount or percentage. Please enter an allocation for at least one selected investor.";
    }

    private static (decimal Amount, decimal? Percentage) ResolveAllocation(InvestmentAllocationRequest allocation, decimal totalCost)
    {
        var amount = allocation.Amount ?? 0m;
        var percentage = allocation.Percentage;

        if (amount > 0 && (percentage == null || percentage <= 0) && totalCost > 0)
            percentage = Math.Round((amount / totalCost) * 100m, 2);
        else if ((amount <= 0 || totalCost <= 0) && percentage is > 0)
            amount = Math.Round(totalCost * (percentage.Value / 100m), 2);

        return (amount, percentage);
    }

    private static void RecalculateInvestmentPercentages(InventoryItem item)
    {
        if (item.Investments == null || !item.Investments.Any())
            return;

        var totalCost = item.CostPrice;
        if (totalCost <= 0)
        {
            foreach (var inv in item.Investments)
                inv.Percentage = null;
            return;
        }

        foreach (var inv in item.Investments)
            inv.Percentage = Math.Round((inv.Amount / totalCost) * 100m, 2);
    }
}
