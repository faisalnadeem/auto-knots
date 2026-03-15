using System.ComponentModel.DataAnnotations;

namespace AutoKnots.Models;

/// <summary>
/// One month's aggregate profit for the chart.
/// </summary>
public class MonthlyProfitRow
{
    public string MonthLabel { get; set; } = string.Empty; // e.g. "Jan 2025"
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalProfit { get; set; }
    public int SoldCount { get; set; }
}

/// <summary>
/// One sold vehicle with its total profit and link to detail.
/// </summary>
public class SoldVehicleProfitRow
{
    public int InventoryItemId { get; set; }
    public string VehicleName { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
    public string DetailsUrl { get; set; } = string.Empty;
    /// <summary>
    /// Per-investor (including creator) profit for this vehicle.
    /// </summary>
    public IList<InvestorProfitLine> InvestorProfits { get; set; } = new List<InvestorProfitLine>();
}

public class InvestorProfitLine
{
    public string InvestorName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class ProfitsDashboardViewModel
{
    [DataType(DataType.Date)]
    public DateTime? DateFrom { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DateTo { get; set; }

    /// <summary>
    /// If set, filter to this month (year + month); overrides DateFrom/DateTo when "month" filter is used.
    /// </summary>
    public int? FilterYear { get; set; }
    public int? FilterMonth { get; set; }

    public decimal TotalProfit { get; set; }
    public int SoldCount { get; set; }

    /// <summary>
    /// For chart: profit by month in the selected range.
    /// </summary>
    public IList<MonthlyProfitRow> MonthlyProfits { get; set; } = new List<MonthlyProfitRow>();

    /// <summary>
    /// Sold vehicles in the range; user can click through to vehicle profit detail.
    /// </summary>
    public IList<SoldVehicleProfitRow> SoldVehicles { get; set; } = new List<SoldVehicleProfitRow>();
}
