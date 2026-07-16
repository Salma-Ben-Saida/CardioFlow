using System.Security.Claims;
using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/procedures")]
[Authorize(Roles = "Admin,Medecin")]
public class ProceduresController : ControllerBase
{
    private readonly ProcedureService _procedures;
    private readonly InventoryService _inventory;

    public ProceduresController(ProcedureService procedures, InventoryService inventory)
    {
        _procedures = procedures;
        _inventory = inventory;
    }

    /// <summary>
    /// POST /api/procedures/lesions - placing a Lesion marker in the
    /// segmentation UI. Creates the ProcedureReport on first use for
    /// this CathStudy if one doesn't exist yet.
    /// </summary>
    [HttpPost("lesions")]
    public async Task<IActionResult> AddLesion([FromBody] CreateLesionDto dto)
    {
        try
        {
            var lesion = await _procedures.AddLesionAsync(dto);
            return CreatedAtAction(nameof(AddLesion), new { id = lesion.Id }, lesion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/procedures/lesions/{lesionId}/stent - only reachable from an
    /// existing Lesion marker, matching the "select lesion, then add treatment"
    /// UI flow. Consumes the chosen InventoryItem as part of this call.
    /// </summary>
    [HttpPost("lesions/{lesionId:guid}/stent")]
    public async Task<IActionResult> AddStentTreatment(Guid lesionId, [FromBody] CreateStentTreatmentDto dto)
    {
        dto.LesionId = lesionId; // route is authoritative over any body value

        try
        {
            var treatment = await _procedures.AddStentTreatmentAsync(dto);
            return Ok(treatment);
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
    /// POST /api/procedures/lesions/{lesionId}/balloon - same rule: only
    /// addable from an existing Lesion, never standalone.
    /// </summary>
    [HttpPost("lesions/{lesionId:guid}/balloon")]
    public async Task<IActionResult> AddBalloonTreatment(Guid lesionId, [FromBody] CreateBalloonTreatmentDto dto)
    {
        dto.LesionId = lesionId;

        try
        {
            var treatment = await _procedures.AddBalloonTreatmentAsync(dto, GetCurrentUserId());
            return Ok(treatment);
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
    var claim = User.FindFirstValue("nameid"); // MapInboundClaims=false → literal JWT claim key, not ClaimTypes.NameIdentifier
    if (Guid.TryParse(claim, out var userId))
        return userId;

    throw new UnauthorizedAccessException("No authenticated user found.");
}
/// <summary>
/// POST /api/procedures/reports/{reportId}/confirm - finalizes the report.
/// Every Reserved device attached to this procedure becomes Consumed.
/// Irreversible.
/// </summary>
[HttpPost("reports/{reportId:guid}/confirm")]
public async Task<IActionResult> ConfirmReport(Guid reportId)
{
    try
    {
        await _procedures.ConfirmReportAsync(reportId, GetCurrentUserId());
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
[HttpPost("confirm/{id}")]
public async Task ConfirmReportAsync(Guid reportId, Guid userId)
{
    var report = await _procedures.GetReportByIdAsync(reportId)
        ?? throw new InvalidOperationException("Report not found");

    if (report.IsConfirmed)
        throw new InvalidOperationException("Report already confirmed");

    await _procedures.ConfirmReportAsync(report.Id, GetCurrentUserId()) ; // wrap in a transaction with the inventory update
}
}