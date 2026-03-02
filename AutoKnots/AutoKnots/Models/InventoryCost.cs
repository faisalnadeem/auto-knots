using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventoryCost
{
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    /// <summary>
    /// Investor who incurred this cost. Optional in case the business
    /// wants to track company-only costs.
    /// </summary>
    public string? InvestorUserId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Amount { get; set; }

    /// <summary>
    /// Human friendly type/category for the cost (e.g. Repair, Registration).
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public InventoryItem? InventoryItem { get; set; }
}

