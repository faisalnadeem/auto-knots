namespace AutoKnots.Models;

public class InvestorDashboardViewModel
{
    public List<InventoryInvestment> PendingInvestments { get; set; } = new();
    public List<InventoryInvestment> ApprovedInvestments { get; set; } = new();
}

