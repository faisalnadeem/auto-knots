using AutoKnots.Data;
using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly IInventoryService _inventoryService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public InventoryController(IInventoryService inventoryService, ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _inventoryService = inventoryService;
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var currentUserId = _userManager.GetUserId(User);
        var result = await _inventoryService.GetListAsync(search, page, DefaultPageSize, currentUserId, cancellationToken);
        ViewBag.Search = search;
        ViewBag.TotalCount = result.TotalCount;
        ViewBag.Page = result.Page;
        ViewBag.PageSize = result.PageSize;
        ViewBag.TotalPages = (int)Math.Ceiling(result.TotalCount / (double)result.PageSize);
        return View(result.Items);
    }

    [HttpGet]
    public IActionResult Add()
    {
        var currentUserId = _userManager.GetUserId(User);
        var investors = _userManager.Users
            .Where(u => u.Id != currentUserId)
            .OrderBy(u => u.Email)
            .ToList();

        ViewBag.Investors = investors;
        return View(new InventoryItem { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CancellationToken cancellationToken = default)
    {
        // Manually map form values to avoid any model binding quirks
        var form = Request.Form;
        var investorIds = form["investorIds"];

        var item = new InventoryItem();

        item.Name = form["Name"];
        item.Make = form["Make"];
        item.Model = form["Model"];
        item.Variant = form["Variant"];
        item.EngineNumber = form["EngineNumber"];
        item.ChassisNumber = form["ChassisNumber"];

        if (DateTime.TryParse(form["PurchaseDate"], out var purchaseDate))
        {
            item.PurchaseDate = purchaseDate;
        }

        if (decimal.TryParse(form["CostPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cost))
        {
            item.CostPrice = cost;
        }

        if (decimal.TryParse(form["SalePrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sale))
        {
            item.SalePrice = sale;
        }

        // Checkbox posts value only when checked; presence means true
        item.IsActive = form.ContainsKey("IsActive");
        item.CreatedByUserId = _userManager.GetUserId(User);

        // If investors are selected, start the workflow in PendingApproval and keep inactive.
        if (investorIds.Count > 0)
        {
            item.Status = InventoryStatus.PendingApproval;
            item.IsActive = false;
        }

        var result = await _inventoryService.CreateAsync(item, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to create.");
            return View(item);
        }

        // Create investment records for selected investors.
        if (investorIds.Count > 0 && result.Item != null)
        {
            var investments = new List<InventoryInvestment>();
            var totalCost = result.Item.CostPrice;

            foreach (var investorId in investorIds)
            {
                decimal amount = 0;
                decimal? percentage = null;

                var amountKey = $"amount_{investorId}";
                var percentageKey = $"percentage_{investorId}";

                if (decimal.TryParse(form[amountKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt > 0)
                {
                    amount = amt;
                }

                if (decimal.TryParse(form[percentageKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var perc) && perc > 0)
                {
                    percentage = perc;
                }

                // If user provided only amount, calculate percentage from cost price.
                if (amount > 0 && (percentage == null || percentage <= 0) && totalCost > 0)
                {
                    percentage = Math.Round((amount / totalCost) * 100m, 2);
                }
                // If user provided only percentage, calculate amount from cost price.
                else if ((amount <= 0 || totalCost <= 0) && percentage is > 0)
                {
                    amount = Math.Round(totalCost * (percentage.Value / 100m), 2);
                }
                // If both are zero / missing, skip.
                if (amount <= 0 && (percentage == null || percentage <= 0))
                {
                    continue;
                }

                investments.Add(new InventoryInvestment
                {
                    InventoryItemId = result.Item.Id,
                    InvestorUserId = investorId,
                    Amount = amount,
                    Percentage = percentage,
                    Status = InvestmentStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (investments.Count > 0)
            {
                _db.InventoryInvestments.AddRange(investments);
                await _db.SaveChangesAsync(cancellationToken);
            }
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        var creatorId = item.CreatedByUserId ?? _userManager.GetUserId(User);
        var investors = _userManager.Users
            .Where(u => u.Id != creatorId)
            .OrderBy(u => u.Email)
            .ToList();

        ViewBag.Investors = investors;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryItem model, CancellationToken cancellationToken = default)
    {
        var form = Request.Form;
        var investorIds = form["investorIds"];

        // Ensure we have the correct Id
        if (int.TryParse(form["Id"], out var id))
        {
            model.Id = id;
        }

        model.Name = form["Name"];
        model.Make = form["Make"];
        model.Model = form["Model"];
        model.Variant = form["Variant"];
        model.EngineNumber = form["EngineNumber"];
        model.ChassisNumber = form["ChassisNumber"];

        if (DateTime.TryParse(form["PurchaseDate"], out var purchaseDate))
        {
            model.PurchaseDate = purchaseDate;
        }

        if (decimal.TryParse(form["CostPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cost))
        {
            model.CostPrice = cost;
        }

        if (decimal.TryParse(form["SalePrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sale))
        {
            model.SalePrice = sale;
        }

        model.IsActive = form.ContainsKey("IsActive");

        var result = await _inventoryService.UpdateAsync(model, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to update.");
            return View(model);
        }

        // Update investor allocations if any were provided.
        if (investorIds.Count > 0 && result.Item != null)
        {
            var existingInvestments = await _db.InventoryInvestments
                .Where(x => x.InventoryItemId == result.Item.Id)
                .ToListAsync(cancellationToken);

            var totalCost = result.Item.CostPrice;

            foreach (var investorId in investorIds)
            {
                decimal amount = 0;
                decimal? percentage = null;

                var amountKey = $"amount_{investorId}";
                var percentageKey = $"percentage_{investorId}";

                if (decimal.TryParse(form[amountKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt > 0)
                {
                    amount = amt;
                }

                if (decimal.TryParse(form[percentageKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var perc) && perc > 0)
                {
                    percentage = perc;
                }

                if (amount > 0 && (percentage == null || percentage <= 0) && totalCost > 0)
                {
                    percentage = Math.Round((amount / totalCost) * 100m, 2);
                }
                else if ((amount <= 0 || totalCost <= 0) && percentage is > 0)
                {
                    amount = Math.Round(totalCost * (percentage.Value / 100m), 2);
                }

                if (amount <= 0 && (percentage == null || percentage <= 0))
                {
                    continue;
                }

                var existing = existingInvestments.FirstOrDefault(x => x.InvestorUserId == investorId);
                if (existing != null)
                {
                    existing.Amount = amount;
                    existing.Percentage = percentage;
                }
                else
                {
                    _db.InventoryInvestments.Add(new InventoryInvestment
                    {
                        InventoryItemId = result.Item.Id,
                        InvestorUserId = investorId,
                        Amount = amount,
                        Percentage = percentage,
                        Status = InvestmentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.DeleteAsync(id, cancellationToken);
        if (!result.Success)
            return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
