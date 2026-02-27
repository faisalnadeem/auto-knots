using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventoryInvestment
{
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    public InventoryItem? InventoryItem { get; set; }

    [Required]
    public string InvestorUserId { get; set; } = string.Empty;

    /// <summary>
    /// Fixed investment amount in currency.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Optional percentage of total investment (0-100).
    /// </summary>
    [Range(0, 100)]
    public decimal? Percentage { get; set; }

    public InvestmentStatus Status { get; set; } = InvestmentStatus.Pending;

    public string? RejectReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DecisionAt { get; set; }
}

