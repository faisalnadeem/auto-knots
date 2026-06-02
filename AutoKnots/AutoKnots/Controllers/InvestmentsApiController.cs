using AutoKnots.Models;
using AutoKnots.Models.Api;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoKnots.Controllers;

/// <summary>Pending and approved investment management endpoints.</summary>
[ApiController]
[Route("api/investments")]
[Authorize]
[Produces("application/json")]
public class InvestmentsApiController : ControllerBase
{
    private readonly IInventoryApprovalService _approvalService;
    private readonly UserManager<IdentityUser> _userManager;

    public InvestmentsApiController(IInventoryApprovalService approvalService, UserManager<IdentityUser> userManager)
    {
        _approvalService = approvalService;
        _userManager = userManager;
    }

    /// <summary>Get pending and approved investments for the current user.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(InvestorDashboardViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InvestorDashboardViewModel>> GetDashboard(CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var model = await _approvalService.GetInvestorDashboardAsync(userId, cancellationToken);
        return Ok(model);
    }

    /// <summary>Get details for a specific investment.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InvestmentDetailsViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestmentDetailsViewModel>> GetDetails(int id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var model = await _approvalService.GetInvestmentDetailsAsync(id, userId, cancellationToken);
        if (model == null)
            return NotFound();

        return Ok(model);
    }

    /// <summary>Approve a pending investment.</summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _approvalService.ApproveInvestmentAsync(id, userId, cancellationToken);
        if (!success)
            return BadRequest(new ApiErrorResponse { Error = "Unable to approve this investment." });

        return NoContent();
    }

    /// <summary>Reject a pending investment.</summary>
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectInvestmentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _approvalService.RejectInvestmentAsync(id, userId, request.Reason, cancellationToken);
        if (!success)
            return BadRequest(new ApiErrorResponse { Error = "Unable to reject this investment." });

        return NoContent();
    }
}
