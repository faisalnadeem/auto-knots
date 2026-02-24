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
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
