using AutoKnots.Models;
using AutoKnots.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoKnots.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize]
[Produces("application/json")]
public class InventoryApiController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryApiController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    /// <summary>Get paginated inventory list with optional search.</summary>
    [HttpGet]
    public async Task<ActionResult<InventoryListResult>> GetList([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetListAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>Get inventory item by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<InventoryItem>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryService.GetByIdAsync(id, cancellationToken);
        if (item == null)
            return NotFound();
        return Ok(item);
    }

    /// <summary>Create a new inventory item.</summary>
    [HttpPost]
    public async Task<ActionResult<InventoryItem>> Create([FromBody] InventoryItem item, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.CreateAsync(item, cancellationToken);
        if (!result.Success)
            return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetById), new { id = result.Item!.Id }, result.Item);
    }

    /// <summary>Update an existing inventory item.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<InventoryItem>> Update(int id, [FromBody] InventoryItem item, CancellationToken cancellationToken = default)
    {
        if (id != item.Id)
            return BadRequest(new { error = "Id in URL does not match body." });
        var result = await _inventoryService.UpdateAsync(item, cancellationToken);
        if (!result.Success)
            return result.Error == "Inventory item not found." ? NotFound() : BadRequest(new { error = result.Error });
        return Ok(result.Item);
    }

    /// <summary>Delete an inventory item.</summary>
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.DeleteAsync(id, cancellationToken);
        if (!result.Success)
            return NotFound();
        return NoContent();
    }
}
