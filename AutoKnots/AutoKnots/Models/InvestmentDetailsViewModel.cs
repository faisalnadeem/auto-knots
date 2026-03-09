namespace AutoKnots.Models;

public class InvestmentParticipantRow
{
    public string InvestorUserId { get; set; } = string.Empty;
    public string InvestorEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Percentage { get; set; }
    public InvestmentStatus Status { get; set; }
    public string? RejectReason { get; set; }
}

public class InvestmentDetailsViewModel
{
    public InventoryItem Item { get; set; } = null!;
    public InventoryInvestment CurrentInvestment { get; set; } = null!;
    public string CreatorEmail { get; set; } = string.Empty;
    public List<InvestmentParticipantRow> Participants { get; set; } = new();
}

