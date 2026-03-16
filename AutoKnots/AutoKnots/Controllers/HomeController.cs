using System.Globalization;
using System.Diagnostics;
using System.Security.Claims;
using AutoKnots.Data;
using AutoKnots.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _db;

    public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
    {
        _logger = logger;
        _db = db;
    }

    public async Task<IActionResult> Index(
        DateTime? from,
        DateTime? to,
        int? year,
        int? month,
        CancellationToken cancellationToken = default)
    {
        DateTime dateFrom;
        DateTime dateTo;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (year.HasValue && month.HasValue)
        {
            dateFrom = new DateTime(year.Value, month.Value, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTo = dateFrom.AddMonths(1).AddTicks(-1);
        }
        else if (from.HasValue && to.HasValue)
        {
            dateFrom = from.Value.Date;
            dateTo = to.Value.Date;
            if (dateTo < dateFrom)
                (dateFrom, dateTo) = (dateTo, dateFrom);
        }
        else
        {
            // Default: last 12 months
            dateTo = DateTime.UtcNow.Date;
            dateFrom = dateTo.AddMonths(-12);
        }

        // Backfill: sold vehicles with no Profit Share (e.g. sold before we added creator-only distribution)
        var soldItemIdsWithProfitShare = await _db.InventoryCosts
            .Where(c => c.Type == "Profit Share")
            .Select(c => c.InventoryItemId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var soldWithoutProfitShare = await _db.InventoryItems
            .Where(i => i.Status == InventoryStatus.Sold && !soldItemIdsWithProfitShare.Contains(i.Id))
            .Where(i => !string.IsNullOrEmpty(i.CreatedByUserId))
            .ToListAsync(cancellationToken);
        foreach (var item in soldWithoutProfitShare)
        {
            var profit = item.SalePrice - item.CostPrice;
            if (profit <= 0)
                continue;
            _db.InventoryCosts.Add(new InventoryCost
            {
                InventoryItemId = item.Id,
                InvestorUserId = item.CreatedByUserId,
                Amount = profit,
                Type = "Profit Share",
                Notes = "Creator profit (backfill: no other investors)",
                CreatedAt = item.CreatedAt
            });
        }
        if (soldWithoutProfitShare.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);

        var profitCostsQuery = _db.InventoryCosts
            .Include(c => c.InventoryItem)
            .Where(c => c.Type == "Profit Share" && c.InventoryItem != null && c.InventoryItem.Status == InventoryStatus.Sold)
            .Where(c => c.CreatedAt >= dateFrom && c.CreatedAt <= dateTo.AddDays(1));

        if (!string.IsNullOrEmpty(currentUserId))
        {
            // Only show profits where the current user is either an investor or the creator
            profitCostsQuery = profitCostsQuery.Where(c =>
                c.InvestorUserId == currentUserId ||
                (c.InventoryItem != null && c.InventoryItem.CreatedByUserId == currentUserId));
        }

        var profitCosts = await profitCostsQuery.ToListAsync(cancellationToken);

        var itemIds = profitCosts.Select(c => c.InventoryItemId).Distinct().ToList();
        var items = await _db.InventoryItems
            .Where(i => itemIds.Contains(i.Id))
            .ToDictionaryAsync(i => i.Id, cancellationToken);

        var userIds = profitCosts.Select(c => c.InvestorUserId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? u.Email ?? u.Id, cancellationToken);

        var byMonth = profitCosts
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .Select(g => new MonthlyProfitRow
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthLabel = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", CultureInfo.CurrentCulture),
                TotalProfit = g.Sum(x => x.Amount),
                SoldCount = g.Select(x => x.InventoryItemId).Distinct().Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToList();

        var byItem = profitCosts
            .GroupBy(c => c.InventoryItemId)
            .Select(g =>
            {
                var first = g.First();
                var item = first.InventoryItem ?? items.GetValueOrDefault(first.InventoryItemId);
                var saleDate = g.Min(c => c.CreatedAt);
                var totalProfit = g.Sum(c => c.Amount);
                var investorLines = g
                    .GroupBy(c => c.InvestorUserId)
                    .Where(ig => !string.IsNullOrEmpty(ig.Key))
                    .Select(ig => new InvestorProfitLine
                    {
                        InvestorName = users.TryGetValue(ig.Key!, out var name) ? name : ig.Key!,
                        Amount = ig.Sum(c => c.Amount),
                        Percentage = totalProfit > 0 ? Math.Round((ig.Sum(c => c.Amount) / totalProfit) * 100m, 2) : 0
                    })
                    .OrderByDescending(x => x.Amount)
                    .ToList();
                return new SoldVehicleProfitRow
                {
                    InventoryItemId = first.InventoryItemId,
                    VehicleName = item?.Name ?? "Vehicle",
                    SaleDate = saleDate,
                    TotalProfit = totalProfit,
                    CostPrice = item?.CostPrice ?? 0,
                    SalePrice = item?.SalePrice ?? 0,
                    DetailsUrl = Url.Action("Profit", "Inventory", new { id = first.InventoryItemId }) ?? "#",
                    InvestorProfits = investorLines
                };
            })
            .OrderByDescending(x => x.SaleDate)
            .ToList();

        var model = new ProfitsDashboardViewModel
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            FilterYear = year,
            FilterMonth = month,
            TotalProfit = byMonth.Sum(x => x.TotalProfit),
            SoldCount = byItem.Count,
            MonthlyProfits = byMonth,
            SoldVehicles = byItem
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
