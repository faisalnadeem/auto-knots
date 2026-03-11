using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventorySaleViewModel
{
    [Required]
    public int InventoryItemId { get; set; }

    [Display(Name = "Vehicle")]
    public string InventoryName { get; set; } = string.Empty;

    [Display(Name = "Total Cost")]
    [DataType(DataType.Currency)]
    public decimal CurrentCost { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Selling price must be 0 or greater.")]
    [Display(Name = "Selling Price")]
    [DataType(DataType.Currency)]
    public decimal SellingPrice { get; set; }
}

