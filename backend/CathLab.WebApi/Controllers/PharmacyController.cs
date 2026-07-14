using System.Security.Claims;
using CathLab.Application.DTOs.Pharmacy;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/pharmacy")]
public class PharmacyController : ControllerBase
{
    private readonly InventoryService _inventory;

    public PharmacyController(InventoryService inventory)
    {
        _inventory = inventory;
    }

    /// <summary>GET /api/pharmacy/products?search=&page=1&pageSize=20</summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _inventory.GetProductsAsync(search, page, pageSize);
        return Ok(result);
    }

    /// <summary>GET /api/pharmacy/products/low-stock - products at or below MinimumStockLevel.</summary>
    [HttpGet("products/low-stock")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        var result = await _inventory.GetLowStockAsync();
        return Ok(result);
    }

    /// <summary>POST /api/pharmacy/products - add a new catalog product.</summary>
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateInventoryProductDto dto)
    {
        try
        {
            var product = await _inventory.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProducts), new { }, product);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>POST /api/pharmacy/items/receive - register one new physical unit into stock.</summary>
    [HttpPost("items/receive")]
    public async Task<IActionResult> ReceiveItem([FromBody] ReceiveInventoryItemDto dto)
    {
        try
        {
            var item = await _inventory.ReceiveItemAsync(dto, GetCurrentUserId());
            return Ok(item);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/pharmacy/items/{id}/reserve - called the moment a cardiologist
    /// selects this item in the segmentation UI, before the report is finalized.
    /// </summary>
    [HttpPost("items/{id:guid}/reserve")]
    public async Task<IActionResult> ReserveItem(Guid id, [FromBody] ReserveItemRequest request)
    {
        try
        {
            await _inventory.ReserveItemAsync(id, request.CathStudyId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>POST /api/pharmacy/items/{id}/release - deselected before opening, back to InStock.</summary>
    [HttpPost("items/{id:guid}/release")]
    public async Task<IActionResult> ReleaseReservation(Guid id)
    {
        try
        {
            await _inventory.ReleaseReservationAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>POST /api/pharmacy/items/{id}/waste - opened but never implanted.</summary>
    [HttpPost("items/{id:guid}/waste")]
    public async Task<IActionResult> MarkWasted(Guid id, [FromBody] MarkWastedRequest request)
    {
        try
        {
            await _inventory.MarkWastedAsync(id, request.Note, GetCurrentUserId());
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>GET /api/pharmacy/items/expiring?withinDays=30</summary>
    [HttpGet("items/expiring")]
    public async Task<IActionResult> GetExpiringItems([FromQuery] int withinDays = 30)
    {
        var items = await _inventory.GetExpiringItemsAsync(withinDays);
        return Ok(items);
    }

    [HttpGet("items/reserved/{cathStudyId:guid}")]
public async Task<IActionResult> GetReservedItems(Guid cathStudyId)
{
    var items = await _inventory.GetReservedItemsForStudyAsync(cathStudyId);
    return Ok(items);
}

[HttpPost("items/receive-batch")]
    [Authorize(Roles = "Admin,ResponsablePharmacie")] 
    public async Task<IActionResult> ReceiveBatch([FromBody] ReceiveInventoryBatchDto dto)
    {
        try
        {
            // Récupère l'ID directement depuis la propriété User du ControllerBase
            Guid currentUserId = GetCurrentUserId();

            var items = await _inventory.ReceiveBatchAsync(dto, currentUserId);
            return Ok(items);
        }
        catch (InvalidOperationException ex) 
        { 
            return Conflict(new { error = ex.Message }); 
        }
        catch (UnauthorizedAccessException ex) 
        { 
            return Unauthorized(new { error = ex.Message }); 
        }
    }
    private Guid GetCurrentUserId()
{
    var userPrincipal = User;

    if (userPrincipal?.Identity?.IsAuthenticated != true)
    {
        throw new UnauthorizedAccessException("No authenticated user found.");
    }

    var userIdClaim = userPrincipal.FindFirst("nameid")?.Value
                      ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? userPrincipal.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
    {
        throw new UnauthorizedAccessException("User identification token is invalid.");
    }

    return userId;
}


public class ReserveItemRequest
{
    public Guid CathStudyId { get; set; }
}

public class MarkWastedRequest
{
    public string? Note { get; set; }
}}