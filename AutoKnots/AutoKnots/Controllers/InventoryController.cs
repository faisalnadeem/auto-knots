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

    private static void RecalculateInvestmentPercentages(InventoryItem item)
    {
        if (item.Investments == null || !item.Investments.Any())
        {
            return;
        }

        var totalCost = item.CostPrice;
        if (totalCost <= 0)
        {
            foreach (var inv in item.Investments)
            {
                inv.Percentage = null;
            }
            return;
        }

        foreach (var inv in item.Investments)
        {
            inv.Percentage = Math.Round((inv.Amount / totalCost) * 100m, 2);
        }
    }

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

        // Validate that if any investors are selected, at least one has a non-zero
        // amount or percentage so we don't silently drop all allocations.
        if (investorIds.Count > 0)
        {
            var hasValidAllocation = false;
            foreach (var investorId in investorIds)
            {
                var amountKey = $"amount_{investorId}";
                var percentageKey = $"percentage_{investorId}";

                if (decimal.TryParse(form[amountKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt > 0)
                {
                    hasValidAllocation = true;
                    break;
                }

                if (decimal.TryParse(form[percentageKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var perc) && perc > 0)
                {
                    hasValidAllocation = true;
                    break;
                }
            }

            if (!hasValidAllocation)
            {
                ModelState.AddModelError(string.Empty,
                    "You selected investors but did not enter any amount or percentage. Please enter an allocation for at least one selected investor.");

                var currentUserIdForView = _userManager.GetUserId(User);
                var investorsForView = _userManager.Users
                    .Where(u => u.Id != currentUserIdForView)
                    .OrderBy(u => u.Email)
                    .ToList();

                ViewBag.Investors = investorsForView;
                return View(new InventoryItem { IsActive = true });
            }
        }

        var item = new InventoryItem();

        item.Name = form["Name"]!;
        item.Make = form["Make"]!;
        item.Model = form["Model"]!;
        item.Variant = form["Variant"];
        item.EngineNumber = form["EngineNumber"]!;
        item.ChassisNumber = form["ChassisNumber"]!;

        if (DateTime.TryParse(form["PurchaseDate"], out var purchaseDate))
        {
            item.PurchaseDate = purchaseDate;
        }

        if (decimal.TryParse(form["CostPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cost))
        {
            item.CostPrice = cost;
        }

        // Sale price is not set at creation time; it can be configured later from Edit.

        item.CreatedByUserId = _userManager.GetUserId(User);

        // If investors are selected, start the workflow in PendingApproval and keep inactive.
        if (investorIds.Count > 0)
        {
            item.Status = InventoryStatus.PendingApproval;
            item.IsActive = false;
        }
        else
        {
            // No investors: vehicle is immediately active.
            item.Status = InventoryStatus.Active;
            item.IsActive = true;
        }

        var result = await _inventoryService.CreateAsync(item, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to create.");
            var currentUserIdForView = _userManager.GetUserId(User);
            var investorsForView = _userManager.Users
                .Where(u => u.Id != currentUserIdForView)
                .OrderBy(u => u.Email)
                .ToList();

            ViewBag.Investors = investorsForView;
            return View(item);
        }

        // Create initial investment and costing records.
        if (result.Item != null)
        {
            var totalCost = result.Item.CostPrice;
            var investments = new List<InventoryInvestment>();
            var costs = new List<InventoryCost>();
            decimal totalInvestorAmount = 0m;

            if (investorIds.Count > 0)
            {
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

                    totalInvestorAmount += amount;

                    investments.Add(new InventoryInvestment
                    {
                        InventoryItemId = result.Item.Id,
                        InvestorUserId = investorId!,
                        Amount = amount,
                        Percentage = percentage,
                        Status = InvestmentStatus.Pending,
                        CreatedAt = DateTime.UtcNow
                    });

                    costs.Add(new InventoryCost
                    {
                        InventoryItemId = result.Item.Id,
                        InvestorUserId = investorId!,
                        Amount = amount,
                        Type = "Initial Investment",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Creator's share is whatever remains of the cost price.
            var creatorId = result.Item.CreatedByUserId;
            var creatorShare = totalCost - totalInvestorAmount;
            if (creatorShare > 0 && !string.IsNullOrEmpty(creatorId))
            {
                costs.Add(new InventoryCost
                {
                    InventoryItemId = result.Item.Id,
                    InvestorUserId = creatorId,
                    Amount = creatorShare,
                    Type = "Initial Investment",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (investments.Count > 0)
            {
                _db.InventoryInvestments.AddRange(investments);
            }

            if (costs.Count > 0)
            {
                _db.InventoryCosts.AddRange(costs);
            }

            if (investments.Count > 0 || costs.Count > 0)
            {
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
        var investorsQuery = _userManager.Users.AsQueryable();
        if (!string.IsNullOrEmpty(creatorId))
        {
            investorsQuery = investorsQuery.Where(u => u.Id != creatorId);
        }
        var investors = investorsQuery
            .OrderBy(u => u.Email)
            .ToList();

        ViewBag.Investors = investors;
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryItem inventoryItem, CancellationToken cancellationToken = default)
    {
        var form = Request.Form;
        var investorIds = form["investorIds"];

        // Ensure we have the correct Id
        if (int.TryParse(form["Id"], out var id))
        {
            inventoryItem.Id = id;
        }

        inventoryItem.Name = form["Name"]!;
        inventoryItem.Make = form["Make"]!;
        inventoryItem.Model = form["Model"]!;
        inventoryItem.Variant = form["Variant"];
        inventoryItem.EngineNumber = form["EngineNumber"]!;
        inventoryItem.ChassisNumber = form["ChassisNumber"]!;

        if (DateTime.TryParse(form["PurchaseDate"], out var purchaseDate))
        {
            inventoryItem.PurchaseDate = purchaseDate;
        }

        if (decimal.TryParse(form["CostPrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var cost))
        {
            inventoryItem.CostPrice = cost;
        }

        if (decimal.TryParse(form["SalePrice"], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var sale))
        {
            inventoryItem.SalePrice = sale;
        }

        // If investors are selected, ensure at least one has a non-zero amount or
        // percentage so allocations are not silently ignored.
        if (investorIds.Count > 0)
        {
            var hasValidAllocation = false;
            foreach (var investorId in investorIds)
            {
                var amountKey = $"amount_{investorId}";
                var percentageKey = $"percentage_{investorId}";

                if (decimal.TryParse(form[amountKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt) && amt > 0)
                {
                    hasValidAllocation = true;
                    break;
                }

                if (decimal.TryParse(form[percentageKey], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var perc) && perc > 0)
                {
                    hasValidAllocation = true;
                    break;
                }
            }

            if (!hasValidAllocation)
            {
                ModelState.AddModelError(string.Empty,
                    "You selected investors but did not enter any amount or percentage. Please enter an allocation for at least one selected investor.");

                var creatorIdForView = inventoryItem.CreatedByUserId ?? _userManager.GetUserId(User);
                var investorsQueryForView = _userManager.Users.AsQueryable();
                if (!string.IsNullOrEmpty(creatorIdForView))
                {
                    investorsQueryForView = investorsQueryForView.Where(u => u.Id != creatorIdForView);
                }

                var investorsForView = investorsQueryForView
                    .OrderBy(u => u.Email)
                    .ToList();

                ViewBag.Investors = investorsForView;
                return View(inventoryItem);
            }
        }

        // If any investors are selected, move the item into PendingApproval
        // so it waits for their decision; otherwise keep existing status.
        if (investorIds.Count > 0)
        {
            inventoryItem.Status = InventoryStatus.PendingApproval;
        }

        var result = await _inventoryService.UpdateAsync(inventoryItem, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to update.");
            return View(inventoryItem);
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
                        InvestorUserId = investorId!,
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

    [HttpGet]
    public async Task<IActionResult> AddCost(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var investorIds = item.Investments
            .Select(x => x.InvestorUserId)
            .Where(id => id != null)
            .Select(id => id!)
            .ToList();

        var creatorId = item.CreatedByUserId;
        if (!string.IsNullOrEmpty(creatorId) && !investorIds.Contains(creatorId))
        {
            investorIds.Add(creatorId);
        }

        var investors = _userManager.Users
            .Where(u => investorIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .ToList();

        ViewBag.Investors = investors;

        var vm = new InventoryCostFormViewModel
        {
            InventoryItemId = item.Id,
            InventoryName = item.Name
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCost(InventoryCostFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var itemForView = await _db.InventoryItems
                .Include(i => i.Investments)
                .FirstOrDefaultAsync(i => i.Id == model.InventoryItemId, cancellationToken);

            var investorIdsView = itemForView?.Investments
                .Select(x => x.InvestorUserId)
                .Where(id => id != null)
                .Select(id => id!)
                .ToList() ?? new List<string>();

            var creatorIdView = itemForView?.CreatedByUserId;
            if (!string.IsNullOrEmpty(creatorIdView) && !investorIdsView.Contains(creatorIdView))
            {
                investorIdsView.Add(creatorIdView);
            }

            var investorsView = _userManager.Users
                .Where(u => investorIdsView.Contains(u.Id))
                .OrderBy(u => u.Email)
                .ToList();
            ViewBag.Investors = investorsView;
            return View(model);
        }

        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == model.InventoryItemId, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var investment = item.Investments.FirstOrDefault(x => x.InvestorUserId == model.InvestorUserId);
        var isCreator = !string.IsNullOrEmpty(item.CreatedByUserId) &&
                        item.CreatedByUserId == model.InvestorUserId;

        // If neither an investment nor the creator, block the operation.
        if (investment == null && !isCreator)
        {
            ModelState.AddModelError("", "Selected investor is not associated with this vehicle.");

            var investorIds = item.Investments
                .Select(x => x.InvestorUserId)
                .Where(id => id != null)
                .Select(id => id!)
                .ToList();

            if (!string.IsNullOrEmpty(item.CreatedByUserId) && !investorIds.Contains(item.CreatedByUserId))
            {
                investorIds.Add(item.CreatedByUserId);
            }

            var investors = _userManager.Users
                .Where(u => investorIds.Contains(u.Id))
                .OrderBy(u => u.Email)
                .ToList();

            ViewBag.Investors = investors;
            return View(model);
        }

        var cost = new InventoryCost
        {
            InventoryItemId = item.Id,
            InvestorUserId = model.InvestorUserId,
            Amount = model.Amount,
            Type = model.Type,
            Notes = model.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.InventoryCosts.Add(cost);

        // Update totals and investor share.
        item.CostPrice += model.Amount;
        if (investment != null)
        {
            investment.Amount += model.Amount;
        }

        RecalculateInvestmentPercentages(item);

        await _db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Cost added successfully and investor allocation updated.";
        return RedirectToAction(nameof(Edit), new { id = item.Id });
    }

    [HttpGet]
    public async Task<IActionResult> EditCosts(int id, CancellationToken cancellationToken = default)
    {
        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var costs = await _db.InventoryCosts
            .Where(c => c.InventoryItemId == id)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        // If no costs exist yet, backfill initial investments (investors + creator share)
        if (!costs.Any())
        {
            var newCosts = new List<InventoryCost>();
            var totalInvestorAmount = 0m;

            if (item.Investments != null && item.Investments.Any())
            {
                foreach (var inv in item.Investments)
                {
                    totalInvestorAmount += inv.Amount;

                    if (inv.Amount > 0 && !string.IsNullOrEmpty(inv.InvestorUserId))
                    {
                        newCosts.Add(new InventoryCost
                        {
                            InventoryItemId = item.Id,
                            InvestorUserId = inv.InvestorUserId,
                            Amount = inv.Amount,
                            Type = "Initial Investment",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            var creatorId = item.CreatedByUserId;
            var creatorShare = item.CostPrice - totalInvestorAmount;
            if (creatorShare > 0 && !string.IsNullOrEmpty(creatorId))
            {
                newCosts.Add(new InventoryCost
                {
                    InventoryItemId = item.Id,
                    InvestorUserId = creatorId,
                    Amount = creatorShare,
                    Type = "Initial Investment",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (newCosts.Count > 0)
            {
                _db.InventoryCosts.AddRange(newCosts);
                await _db.SaveChangesAsync(cancellationToken);
                costs = newCosts
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList();
            }
        }

        var investorIds = costs
            .Select(c => c.InvestorUserId)
            .Where(id => id != null)
            .Select(id => id!)
            .Distinct()
            .ToList();

        var investors = _userManager.Users
            .Where(u => investorIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.UserName);

        ViewBag.InventoryName = item.Name;
        ViewBag.InvestorNames = investors;

        return View(costs);
    }

    [HttpGet]
    public async Task<IActionResult> EditCost(int id, CancellationToken cancellationToken = default)
    {
        var cost = await _db.InventoryCosts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (cost == null)
        {
            return NotFound();
        }

        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == cost.InventoryItemId, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var investorIds = item.Investments
            .Select(x => x.InvestorUserId)
            .Where(id => id != null)
            .Select(id => id!)
            .Distinct()
            .ToList();
        var investors = _userManager.Users
            .Where(u => investorIds.Contains(u.Id))
            .OrderBy(u => u.Email)
            .ToList();

        ViewBag.Investors = investors;

        var vm = new InventoryCostFormViewModel
        {
            Id = cost.Id,
            InventoryItemId = cost.InventoryItemId,
            InvestorUserId = cost.InvestorUserId ?? string.Empty,
            Amount = cost.Amount,
            Type = cost.Type,
            Notes = cost.Notes,
            InventoryName = item.Name
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCost(InventoryCostFormViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var itemForView = await _db.InventoryItems
                .Include(i => i.Investments)
                .FirstOrDefaultAsync(i => i.Id == model.InventoryItemId, cancellationToken);

            var investorIdsView = itemForView?.Investments
                .Select(x => x.InvestorUserId)
                .Where(id => id != null)
                .Select(id => id!)
                .Distinct()
                .ToList() ?? new List<string>();

            var investorsView = _userManager.Users
                .Where(u => investorIdsView.Contains(u.Id))
                .OrderBy(u => u.Email)
                .ToList();
            ViewBag.Investors = investorsView;
            return View(model);
        }

        var cost = await _db.InventoryCosts
            .FirstOrDefaultAsync(c => c.Id == model.Id, cancellationToken);

        if (cost == null)
        {
            return NotFound();
        }

        var item = await _db.InventoryItems
            .Include(i => i.Investments)
            .FirstOrDefaultAsync(i => i.Id == cost.InventoryItemId, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        var previousAmount = cost.Amount;
        var previousInvestorId = cost.InvestorUserId;

        cost.InvestorUserId = model.InvestorUserId;
        cost.Amount = model.Amount;
        cost.Type = model.Type;
        cost.Notes = model.Notes;
        cost.UpdatedAt = DateTime.UtcNow;

        // Adjust inventory cost price by the delta.
        var delta = model.Amount - previousAmount;
        item.CostPrice += delta;

        // Update investor allocations.
        if (previousInvestorId != null)
        {
            var oldInvestment = item.Investments.FirstOrDefault(x => x.InvestorUserId == previousInvestorId);
            if (oldInvestment != null)
            {
                oldInvestment.Amount -= previousAmount;
            }
        }

        var newInvestment = item.Investments.FirstOrDefault(x => x.InvestorUserId == model.InvestorUserId);
        if (newInvestment != null)
        {
            newInvestment.Amount += model.Amount;
        }

        RecalculateInvestmentPercentages(item);

        await _db.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = "Cost updated successfully and investor allocation adjusted.";
        return RedirectToAction(nameof(EditCosts), new { id = item.Id });
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
