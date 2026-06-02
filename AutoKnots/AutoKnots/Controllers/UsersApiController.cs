using AutoKnots.Models.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoKnots.Controllers;

/// <summary>User lookup endpoints for selecting investors.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersApiController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;

    public UsersApiController(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager;
    }

    /// <summary>List users available to be added as investors (excludes current user).</summary>
    [HttpGet("investors")]
    [ProducesResponseType(typeof(IList<InvestorUserDto>), StatusCodes.Status200OK)]
    public IActionResult GetInvestors()
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var investors = _userManager.Users
            .Where(u => u.Id != currentUserId)
            .OrderBy(u => u.Email)
            .Select(u => new InvestorUserDto
            {
                Id = u.Id,
                Email = u.Email
            })
            .ToList();

        return Ok(investors);
    }
}
