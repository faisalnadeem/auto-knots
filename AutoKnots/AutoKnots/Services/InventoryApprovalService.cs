using AutoKnots.Data;
using AutoKnots.Models;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Services;

public class InventoryApprovalService : IInventoryApprovalService
{
    private readonly ApplicationDbContext _db;

    public InventoryApprovalService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<InvestorDashboardViewModel> GetInvestorDashboardAsync(string investorUserId, CancellationToken cancellationToken = default)
    {
        var pending = await _db.InventoryInvestments
            .Include(x => x.InventoryItem)
            .Where(x => x.InvestorUserId == investorUserId && x.Status == InvestmentStatus.Pending)
            .ToListAsync(cancellationToken);

        var approved = await _db.InventoryInvestments
            .Include(x => x.InventoryItem)
            .Where(x => x.InvestorUserId == investorUserId && x.Status == InvestmentStatus.Approved)
            .ToListAsync(cancellationToken);

        return new InvestorDashboardViewModel
        {
            PendingInvestments = pending,
            ApprovedInvestments = approved
        };
    }

    public async Task<bool> ApproveInvestmentAsync(int investmentId, string investorUserId, CancellationToken cancellationToken = default)
    {
        var investment = await _db.InventoryInvestments
            .Include(x => x.InventoryItem)
            .ThenInclude(i => i!.Investments)
            .FirstOrDefaultAsync(x => x.Id == investmentId && x.InvestorUserId == investorUserId, cancellationToken);

        if (investment == null || investment.Status != InvestmentStatus.Pending)
            return false;

        investment.Status = InvestmentStatus.Approved;
        investment.DecisionAt = DateTime.UtcNow;
        investment.RejectReason = null;

        await UpdateInventoryStatusForItemAsync(investment.InventoryItem!, cancellationToken);

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RejectInvestmentAsync(int investmentId, string investorUserId, string reason, CancellationToken cancellationToken = default)
    {
        var investment = await _db.InventoryInvestments
            .Include(x => x.InventoryItem)
            .ThenInclude(i => i!.Investments)
            .FirstOrDefaultAsync(x => x.Id == investmentId && x.InvestorUserId == investorUserId, cancellationToken);

        if (investment == null || investment.Status != InvestmentStatus.Pending)
            return false;

        investment.Status = InvestmentStatus.Rejected;
        investment.DecisionAt = DateTime.UtcNow;
        investment.RejectReason = reason;

        if (investment.InventoryItem != null)
        {
            // Any rejection moves the inventory item to Failed and it cannot revert.
            investment.InventoryItem.Status = InventoryStatus.Failed;
            investment.InventoryItem.IsActive = false;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<InvestmentDetailsViewModel?> GetInvestmentDetailsAsync(int investmentId, string investorUserId, CancellationToken cancellationToken = default)
    {
        var investment = await _db.InventoryInvestments
            .Include(x => x.InventoryItem)
            .ThenInclude(i => i!.Investments)
            .FirstOrDefaultAsync(x => x.Id == investmentId && x.InvestorUserId == investorUserId, cancellationToken);

        if (investment == null || investment.InventoryItem == null)
        {
            return null;
        }

        var item = investment.InventoryItem;

        var userIds = item.Investments
            .Select(x => x.InvestorUserId)
            .Append(item.CreatedByUserId ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var participants = new List<InvestmentParticipantRow>();
        foreach (var inv in item.Investments)
        {
            var user = users.FirstOrDefault(u => u.Id == inv.InvestorUserId);
            participants.Add(new InvestmentParticipantRow
            {
                InvestorUserId = inv.InvestorUserId,
                InvestorEmail = user?.Email ?? inv.InvestorUserId,
                Amount = inv.Amount,
                Percentage = inv.Percentage,
                Status = inv.Status,
                RejectReason = inv.RejectReason
            });
        }

        var creator = !string.IsNullOrWhiteSpace(item.CreatedByUserId)
            ? users.FirstOrDefault(u => u.Id == item.CreatedByUserId)
            : null;

        return new InvestmentDetailsViewModel
        {
            Item = item,
            CurrentInvestment = investment,
            CreatorEmail = creator?.Email ?? "N/A",
            Participants = participants
        };
    }

    private static async Task UpdateInventoryStatusForItemAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        // If item has already failed, do not change status.
        if (item.Status == InventoryStatus.Failed)
        {
            return;
        }

        // If any investment is rejected, item immediately fails.
        if (item.Investments.Any(x => x.Status == InvestmentStatus.Rejected))
        {
            item.Status = InventoryStatus.Failed;
            item.IsActive = false;
            return;
        }

        // If all are approved, the item moves to Active.
        if (item.Investments.Any() && item.Investments.All(x => x.Status == InvestmentStatus.Approved))
        {
            item.Status = InventoryStatus.Active;
            item.IsActive = true;
            return;
        }

        // Otherwise, keep in PendingApproval.
        item.Status = InventoryStatus.PendingApproval;
        item.IsActive = false;
        await Task.CompletedTask;
    }
}

