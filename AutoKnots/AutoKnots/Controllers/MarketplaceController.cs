using AutoKnots.Data;
using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoKnots.Controllers;

public class MarketplaceController : Controller
{
    private readonly IMarketplaceService _marketplace;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<IdentityUser> _userManager;

    public MarketplaceController(IMarketplaceService marketplace, ApplicationDbContext db, UserManager<IdentityUser> userManager)
    {
        _marketplace = marketplace;
        _db = db;
        _userManager = userManager;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index([FromQuery] MarketplaceSearch filters, CancellationToken cancellationToken = default)
    {
        ViewBag.Search = filters;
        return View(await _marketplace.SearchAsync(filters, cancellationToken));
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var listing = await _marketplace.GetPublicDetailsAsync(id, cancellationToken);
        return listing == null ? NotFound() : View(listing);
    }

    [Authorize]
    public async Task<IActionResult> Mine([FromQuery] MarketplaceSearch filters, CancellationToken cancellationToken = default)
    {
        var userId = _userManager.GetUserId(User)!;
        filters.PageSize = 20;
        return View(await _marketplace.GetSellerListingsAsync(userId, filters, cancellationToken));
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Create(CancellationToken cancellationToken = default)
    {
        await PopulateInventoryAsync(null, cancellationToken);
        return View(new MarketplaceListingForm { Year = DateTime.UtcNow.Year });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MarketplaceListingForm form, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            await PopulateInventoryAsync(form.InventoryItemId, cancellationToken);
            return View(form);
        }

        var result = await _marketplace.CreateAsync(_userManager.GetUserId(User)!, form.ToInput(), cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateInventoryAsync(form.InventoryItemId, cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = "Marketplace listing created.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpGet, Authorize]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var listing = await _marketplace.GetOwnerDetailsAsync(id, _userManager.GetUserId(User)!, cancellationToken);
        if (listing == null) return NotFound();

        await PopulateInventoryAsync(listing.InventoryItemId, cancellationToken);
        return View(new MarketplaceListingForm
        {
            Id = listing.Id,
            InventoryItemId = listing.InventoryItemId,
            Title = listing.Title,
            Description = listing.Description,
            Price = listing.Price,
            Year = listing.Year,
            Mileage = listing.Mileage,
            FuelType = listing.FuelType,
            Transmission = listing.Transmission,
            BodyStyle = listing.BodyStyle,
            Condition = listing.Condition,
            Location = listing.Location,
            Publish = listing.Status == ListingStatus.Active,
            ImageUrls = string.Join(Environment.NewLine, listing.ImageUrls)
        });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MarketplaceListingForm form, CancellationToken cancellationToken = default)
    {
        if (form.Id != id) return BadRequest();
        if (!ModelState.IsValid)
        {
            await PopulateInventoryAsync(form.InventoryItemId, cancellationToken);
            return View(form);
        }

        var result = await _marketplace.UpdateAsync(id, _userManager.GetUserId(User)!, form.ToInput(), cancellationToken);
        if (!result.Success)
        {
            if (result.Error == "Listing not found.") return NotFound();
            ModelState.AddModelError(string.Empty, result.Error!);
            await PopulateInventoryAsync(form.InventoryItemId, cancellationToken);
            return View(form);
        }

        TempData["SuccessMessage"] = "Marketplace listing updated.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, ListingStatus status, CancellationToken cancellationToken = default)
    {
        if (status is ListingStatus.Draft or ListingStatus.Removed) return BadRequest();
        var result = await _marketplace.SetStatusAsync(id, _userManager.GetUserId(User)!, status, cancellationToken);
        if (!result.Success) TempData["ErrorMessage"] = result.Error;
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken = default)
    {
        var result = await _marketplace.SetStatusAsync(id, _userManager.GetUserId(User)!, ListingStatus.Removed, cancellationToken);
        if (!result.Success) TempData["ErrorMessage"] = result.Error;
        return RedirectToAction(nameof(Mine));
    }

    private async Task PopulateInventoryAsync(int? selectedId, CancellationToken cancellationToken)
    {
        var userId = _userManager.GetUserId(User);
        ViewBag.InventoryItems = await _db.InventoryItems.AsNoTracking()
            .Where(x => x.CreatedByUserId == userId && x.Status != InventoryStatus.Sold &&
                        (x.Listing == null || x.Id == selectedId))
            .OrderBy(x => x.Make).ThenBy(x => x.Model)
            .Select(x => new { x.Id, Label = x.Name + " — " + x.Make + " " + x.Model })
            .ToListAsync(cancellationToken);
    }
}
