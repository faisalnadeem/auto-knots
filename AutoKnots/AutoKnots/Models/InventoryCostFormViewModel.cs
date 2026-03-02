using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventoryCostFormViewModel
{
    public int Id { get; set; }

    [Required]
    public int InventoryItemId { get; set; }

    /// <summary>
    /// Investor who spent this cost.
    /// </summary>
    [Required]
    public string InvestorUserId { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; set; }

    [Required]
    [MaxLength(128)]
    public string Type { get; set; } = string.Empty;

    [MaxLength(512)]
    public string? Notes { get; set; }

    public string? InventoryName { get; set; }
}

