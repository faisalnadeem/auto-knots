using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

public class InventoryProfitRow
{
    public string InvestorUserId { get; set; } = string.Empty;

    [Display(Name = "Investor")]
    public string InvestorName { get; set; } = string.Empty;

    [Display(Name = "Investment %")]
    public decimal Percentage { get; set; }

    [Display(Name = "Total Profit")]
    [DataType(DataType.Currency)]
    public decimal ProfitAmount { get; set; }
}

public class InventoryProfitViewModel
{
    public int InventoryItemId { get; set; }

    [Display(Name = "Vehicle")]
    public string InventoryName { get; set; } = string.Empty;

    [Display(Name = "Total Cost")]
    [DataType(DataType.Currency)]
    public decimal TotalCost { get; set; }

    [Display(Name = "Selling Price")]
    [DataType(DataType.Currency)]
    public decimal SellingPrice { get; set; }

    [Display(Name = "Total Profit")]
    [DataType(DataType.Currency)]
    public decimal TotalProfit { get; set; }

    public IList<InventoryProfitRow> Rows { get; set; } = new List<InventoryProfitRow>();
}

