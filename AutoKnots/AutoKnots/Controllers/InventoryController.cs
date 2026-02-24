using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoKnots.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private const int DefaultPageSize = 10;
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetListAsync(search, page, DefaultPageSize, cancellationToken);
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
        return View(new InventoryItem { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(CancellationToken cancellationToken = default)
    {
        // Manually map form values to avoid any model binding quirks
        var form = Request.Form;

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

        var result = await _inventoryService.CreateAsync(item, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError("", result.Error ?? "Failed to create.");
            return View(item);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryItem model, CancellationToken cancellationToken = default)
    {
        var form = Request.Form;

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
