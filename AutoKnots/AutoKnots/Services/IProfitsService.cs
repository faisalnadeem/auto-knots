using AutoKnots.Models;

namespace AutoKnots.Services;

public interface IProfitsService
{
    Task<ProfitsDashboardViewModel> GetDashboardAsync(
        string? currentUserId,
        DateTime? from,
        DateTime? to,
        int? year,
        int? month,
        CancellationToken cancellationToken = default);
}
