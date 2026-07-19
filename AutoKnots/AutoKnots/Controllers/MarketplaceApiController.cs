using System.Security.Claims;
using AutoKnots.Models;
using AutoKnots.Models.Api;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoKnots.Controllers;

/// <summary>Public vehicle marketplace browsing and seller listing management.</summary>
[ApiController]
[Route("api/marketplace/listings")]
[Produces("application/json")]
public class MarketplaceApiController : ControllerBase
{
    private readonly IMarketplaceService _marketplace;

    public MarketplaceApiController(IMarketplaceService marketplace) => _marketplace = marketplace;

    /// <summary>Browse active marketplace listings.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MarketplacePage), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarketplacePage>> Search(
        [FromQuery] MarketplaceSearch search,
        CancellationToken cancellationToken = default) =>
        Ok(await _marketplace.SearchAsync(search, cancellationToken));

    /// <summary>View an active marketplace listing.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MarketplaceListingDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketplaceListingDetails>> Details(int id, CancellationToken cancellationToken = default)
    {
        var listing = await _marketplace.GetPublicDetailsAsync(id, cancellationToken);
        return listing == null ? NotFound() : Ok(listing);
    }

    /// <summary>List marketplace records owned by the current seller.</summary>
    [HttpGet("mine")]
    [Authorize]
    [ProducesResponseType(typeof(MarketplacePage), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarketplacePage>> Mine(
        [FromQuery] MarketplaceSearch search,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        return userId == null
            ? Unauthorized()
            : Ok(await _marketplace.GetSellerListingsAsync(userId, search, cancellationToken));
    }

    /// <summary>Create a draft or active listing from an owned inventory vehicle.</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(MarketplaceListingDetails), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MarketplaceListingDetails>> Create(
        [FromBody] MarketplaceListingInput input,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _marketplace.CreateAsync(userId, input, cancellationToken);
        if (!result.Success) return BadRequest(new ApiErrorResponse { Error = result.Error! });

        return CreatedAtAction(nameof(Details), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>Edit a listing owned by the current seller.</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType(typeof(MarketplaceListingDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketplaceListingDetails>> Update(
        int id,
        [FromBody] MarketplaceListingInput input,
        CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _marketplace.UpdateAsync(id, userId, input, cancellationToken);
        return ToActionResult(result);
    }

    /// <summary>Activate, pause, or mark an owned listing as sold.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize]
    [ProducesResponseType(typeof(MarketplaceListingDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MarketplaceListingDetails>> SetStatus(
        int id,
        [FromBody] ListingStatusInput input,
        CancellationToken cancellationToken = default)
    {
        if (input.Status is ListingStatus.Draft or ListingStatus.Removed)
            return BadRequest(new ApiErrorResponse { Error = "Use edit or delete for this status transition." });

        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();
        return ToActionResult(await _marketplace.SetStatusAsync(id, userId, input.Status, cancellationToken));
    }

    /// <summary>Remove an owned listing while retaining its history.</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Remove(int id, CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId();
        if (userId == null) return Unauthorized();

        var result = await _marketplace.SetStatusAsync(id, userId, ListingStatus.Removed, cancellationToken);
        if (result.Success) return NoContent();
        return result.Error == "Listing not found."
            ? NotFound()
            : BadRequest(new ApiErrorResponse { Error = result.Error! });
    }

    private ActionResult<MarketplaceListingDetails> ToActionResult(MarketplaceResult<MarketplaceListingDetails> result)
    {
        if (result.Success) return Ok(result.Value);
        return result.Error == "Listing not found."
            ? NotFound()
            : BadRequest(new ApiErrorResponse { Error = result.Error! });
    }

    private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
