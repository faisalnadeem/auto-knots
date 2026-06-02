using AutoKnots.Models;
using AutoKnots.Models.Api;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AutoKnots.Controllers;

/// <summary>Vehicle inventory management endpoints including costs, sales, and profit details.</summary>
[ApiController]
[Route("api/inventory")]
[Authorize]
[Produces("application/json")]
public class InventoryApiController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryOperationsService _operationsService;

    public InventoryApiController(IInventoryService inventoryService, IInventoryOperationsService operationsService)
    {
        _inventoryService = inventoryService;
        _operationsService = operationsService;
    }

    /// <summary>Get paginated inventory list with optional search (scoped to current user).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(InventoryListResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryListResult>> GetList(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var result = await _inventoryService.GetListAsync(search, page, pageSize, currentUserId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get inventory item by id.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InventoryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItem>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        return Ok(item);
    }

    /// <summary>Create a new vehicle with optional investor allocations.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryItem), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryItem>> Create([FromBody] CreateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var item = MapToInventoryItem(request);
        item.CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var result = await _operationsService.CreateWithInvestorsAsync(item, request.Investors, cancellationToken);
        if (!result.Success)
            return BadRequest(new ApiErrorResponse { Error = result.Error ?? "Failed to create inventory item." });

        return CreatedAtAction(nameof(GetById), new { id = result.Item!.Id }, result.Item);
    }

    /// <summary>Update an existing inventory item and optionally update investor allocations.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InventoryItem), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItem>> Update(int id, [FromBody] UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return NotFound();

        var item = MapToInventoryItem(request);
        item.Id = id;
        item.CreatedByUserId = existing.CreatedByUserId;

        var result = await _operationsService.UpdateWithInvestorsAsync(item, request.Investors, cancellationToken);
        if (!result.Success)
            return result.Error == "Inventory item not found." ? NotFound() : BadRequest(new ApiErrorResponse { Error = result.Error ?? "Failed to update." });

        return Ok(result.Item);
    }

    /// <summary>Delete an inventory item.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.DeleteAsync(id, cancellationToken);
        if (!result.Success)
            return NotFound();

        return NoContent();
    }

    /// <summary>Get all cost entries for a vehicle.</summary>
    [HttpGet("{id:int}/costs")]
    [ProducesResponseType(typeof(IList<InventoryCost>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IList<InventoryCost>>> GetCosts(int id, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        var costs = await _operationsService.GetCostsAsync(id, cancellationToken);
        return Ok(costs);
    }

    /// <summary>Add a cost entry to a vehicle (repairs, registration, etc.).</summary>
    [HttpPost("{id:int}/costs")]
    [ProducesResponseType(typeof(InventoryCost), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryCost>> AddCost(int id, [FromBody] AddCostRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        var (success, error, cost) = await _operationsService.AddCostAsync(id, request, cancellationToken);
        if (!success)
            return BadRequest(new ApiErrorResponse { Error = error ?? "Failed to add cost." });

        return CreatedAtAction(nameof(GetCosts), new { id }, cost);
    }

    /// <summary>Update an existing cost entry.</summary>
    [HttpPut("{id:int}/costs/{costId:int}")]
    [ProducesResponseType(typeof(InventoryCost), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryCost>> UpdateCost(int id, int costId, [FromBody] UpdateCostRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        var (success, error, cost) = await _operationsService.UpdateCostAsync(costId, request, cancellationToken);
        if (!success)
            return cost == null ? NotFound() : BadRequest(new ApiErrorResponse { Error = error ?? "Failed to update cost." });

        return Ok(cost);
    }

    /// <summary>Mark a vehicle as sold and distribute profit among investors.</summary>
    [HttpPost("{id:int}/sell")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Sell(int id, [FromBody] SellVehicleRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();

        var (success, error) = await _operationsService.SellVehicleAsync(id, request.SellingPrice, cancellationToken);
        if (!success)
            return BadRequest(new ApiErrorResponse { Error = error ?? "Failed to sell vehicle." });

        return NoContent();
    }

    /// <summary>Get profit breakdown for a sold vehicle.</summary>
    [HttpGet("{id:int}/profit")]
    [ProducesResponseType(typeof(InventoryProfitViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryProfitViewModel>> GetProfitDetail(int id, CancellationToken cancellationToken = default)
    {
        var model = await _operationsService.GetProfitDetailAsync(id, cancellationToken);
        if (model == null)
            return NotFound();

        return Ok(model);
    }

    private static InventoryItem MapToInventoryItem(CreateInventoryRequest request) => new()
    {
        Name = request.Name,
        Make = request.Make,
        Model = request.Model,
        Variant = request.Variant,
        EngineNumber = request.EngineNumber,
        ChassisNumber = request.ChassisNumber,
        PurchaseDate = request.PurchaseDate,
        CostPrice = request.CostPrice
    };

    private static InventoryItem MapToInventoryItem(UpdateInventoryRequest request) => new()
    {
        Name = request.Name,
        Make = request.Make,
        Model = request.Model,
        Variant = request.Variant,
        EngineNumber = request.EngineNumber,
        ChassisNumber = request.ChassisNumber,
        PurchaseDate = request.PurchaseDate,
        CostPrice = request.CostPrice
    };
}
