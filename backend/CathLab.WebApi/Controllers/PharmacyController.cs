using System.Security.Claims;
using CathLab.Application.DTOs.Pharmacy;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/pharmacy")]
[Authorize]
public class PharmacyController : ControllerBase
{
    private readonly InventoryService _inventory;
    private readonly AuditService _audit;


    public PharmacyController(InventoryService inventory, AuditService audit)
    {
        _inventory = inventory;
        _audit = audit;
    }
 
    /// <summary>GET /api/pharmacy/products?search=&page=1&pageSize=20</summary>
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _inventory.GetProductsAsync(search, page, pageSize);
        return Ok(result);
    }

    /// <summary>GET /api/pharmacy/products/low-stock - products at or below MinimumStockLevel.</summary>
    [HttpGet("products/low-stock")]
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    public async Task<IActionResult> GetLowStockProducts()
    {
        var result = await _inventory.GetLowStockAsync();
        return Ok(result);
    }

    /// <summary>POST /api/pharmacy/products - add a new catalog product.</summary>
    [HttpPost("products")]
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateInventoryProductDto dto)
    {
        try
        {
            var product = await _inventory.CreateProductAsync(dto);
            await _audit.LogActionAsync(GetCurrentUserId(), "Create", "InventoryProduct", product.Id, product.ProductName);

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
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    public async Task<IActionResult> ReceiveItem([FromBody] ReceiveInventoryItemDto dto)
    {
        try
        {
            var item = await _inventory.ReceiveItemAsync(dto, GetCurrentUserId());
            await _audit.LogActionAsync(GetCurrentUserId(), "Receive", "InventoryItem", item.Id, dto.LotNumber);

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
    [Authorize(Roles = "Admin,ResponsablePharmacie, Medecin")]
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
    [Authorize(Roles = "Admin,ResponsablePharmacie, Medecin")]
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
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    public async Task<IActionResult> MarkWasted(Guid id, [FromBody] MarkWastedRequest request)
    {
        try
        {
            await _inventory.MarkWastedAsync(id, request.Note, GetCurrentUserId());
            await _audit.LogActionAsync(GetCurrentUserId(), "Waste", "InventoryItem", id, request.Note);
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
    [Authorize(Roles = "Admin,ResponsablePharmacie")]
    public async Task<IActionResult> GetExpiringItems([FromQuery] int withinDays = 30)
    {
        var items = await _inventory.GetExpiringItemsAsync(withinDays);
        return Ok(items);
    }

    [HttpGet("items/reserved/{cathStudyId:guid}")]
    [Authorize(Roles = "Admin,ResponsablePharmacie, Medecin")]
public async Task<IActionResult> GetReservedItems(Guid cathStudyId)
{
    var items = await _inventory.GetReservedItemsForStudyAsync(cathStudyId);
    return Ok(items);
}

[HttpGet("items/available")]
[Authorize(Roles = "Admin,ResponsablePharmacie, Medecin")]
public async Task<IActionResult> GetAvailableItems([FromQuery] string? deviceType)
{
    try
    {
        var items = await _inventory.GetAvailableItemsAsync(deviceType);
        return Ok(items);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
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
            await _audit.LogActionAsync(currentUserId, "ReceiveBatch", "InventoryProduct", dto.ProductId, $"{items.Count} unités");

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
[Authorize(Roles = "Admin,ResponsablePharmacie")]
[HttpGet("items")]
public async Task<IActionResult> GetAllItems([FromQuery] string? status, [FromQuery] string? deviceType)
{
    var items = await _inventory.GetAllItemsAsync(status, deviceType);
    return Ok(items);
}

[Authorize(Roles = "Admin,ResponsablePharmacie")]
[HttpGet("items/reserved")]
public async Task<IActionResult> GetAllReservedItems()
{
    var items = await _inventory.GetAllReservedItemsAsync();
    return Ok(items);
}

public class ReserveItemRequest
{
    public Guid CathStudyId { get; set; }
}

public class MarkWastedRequest
{
    public string? Note { get; set; }
}}