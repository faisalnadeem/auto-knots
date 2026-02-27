using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AutoKnots.Controllers;

[Authorize]
public class InvestmentsController : Controller
{
    private readonly IInventoryApprovalService _approvalService;
    private readonly UserManager<IdentityUser> _userManager;

    public InvestmentsController(IInventoryApprovalService approvalService, UserManager<IdentityUser> userManager)
    {
        _approvalService = approvalService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var model = await _approvalService.GetInvestorDashboardAsync(user.Id, cancellationToken);
        return View(model);
    }

        public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var model = await _approvalService.GetInvestmentDetailsAsync(id, user.Id, cancellationToken);
            if (model == null)
            {
                return NotFound();
            }

            return View(model);
        }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var success = await _approvalService.ApproveInvestmentAsync(id, user.Id, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Unable to approve this investment.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string reason, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        var success = await _approvalService.RejectInvestmentAsync(id, user.Id, reason, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Unable to reject this investment.";
        }
        return RedirectToAction(nameof(Index));
    }
}

