using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoKnots.Controllers;

/// <summary>Profit dashboard and analytics endpoints.</summary>
[ApiController]
[Route("api/profits")]
[Authorize]
[Produces("application/json")]
public class ProfitsApiController : ControllerBase
{
    private readonly IProfitsService _profitsService;

    public ProfitsApiController(IProfitsService profitsService)
    {
        _profitsService = profitsService;
    }

    /// <summary>
    /// Get the profits dashboard. Filter by month/year or custom date range.
    /// Only one filter type should be used at a time.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ProfitsDashboardViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProfitsDashboardViewModel>> GetDashboard(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? year,
        [FromQuery] int? month,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var model = await _profitsService.GetDashboardAsync(userId, from, to, year, month, cancellationToken);
        return Ok(model);
    }
}
