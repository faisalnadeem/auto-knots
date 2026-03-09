using AutoKnots.Models;

namespace AutoKnots.Services;

public interface IInventoryApprovalService
{
    Task<InvestorDashboardViewModel> GetInvestorDashboardAsync(string investorUserId, CancellationToken cancellationToken = default);
    Task<bool> ApproveInvestmentAsync(int investmentId, string investorUserId, CancellationToken cancellationToken = default);
    Task<bool> RejectInvestmentAsync(int investmentId, string investorUserId, string reason, CancellationToken cancellationToken = default);
    Task<InvestmentDetailsViewModel?> GetInvestmentDetailsAsync(int investmentId, string investorUserId, CancellationToken cancellationToken = default);
}

