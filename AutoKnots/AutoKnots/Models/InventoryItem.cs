using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventoryItem
{
    public int Id { get; set; }

    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Make")]
    public string Make { get; set; } = string.Empty;

    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    [Display(Name = "Variant")]
    public string? Variant { get; set; }

    [Display(Name = "Engine Number")]
    public string EngineNumber { get; set; } = string.Empty;

    [Display(Name = "Chassis Number")]
    public string ChassisNumber { get; set; } = string.Empty;

    [Display(Name = "Purchase Date")]
    [DataType(DataType.Date)]
    public DateTime? PurchaseDate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Cost price must be 0 or greater.")]
    [Display(Name = "Cost Price")]
    public decimal CostPrice { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Sale price must be 0 or greater.")]
    [Display(Name = "Sale Price")]
    public decimal SalePrice { get; set; }

    public int? MinimumStock { get; set; }

    /// <summary>
    /// Date the inventory record was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Legacy "active" flag; for new logic prefer the Status property.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Current lifecycle status in the investor approval workflow.
    /// </summary>
    public InventoryStatus Status { get; set; } = InventoryStatus.Draft;

    /// <summary>
    /// User ID (from AspNetUsers) that created this inventory entry.
    /// Used to prevent the creator from approving/rejecting their own investment.
    /// </summary>
    public string? CreatedByUserId { get; set; }

    /// <summary>
    /// Investor allocations and approval decisions for this inventory item.
    /// </summary>
    public ICollection<InventoryInvestment> Investments { get; set; } = new List<InventoryInvestment>();

    public VehicleListing? Listing { get; set; }
}

