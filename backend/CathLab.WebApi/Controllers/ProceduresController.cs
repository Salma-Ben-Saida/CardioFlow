using System.Security.Claims;
using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/procedures")]
public class ProceduresController : ControllerBase
{
    private readonly ProcedureService _procedures;

    public ProceduresController(ProcedureService procedures)
    {
        _procedures = procedures;
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
            var treatment = await _procedures.AddStentTreatmentAsync(dto, GetCurrentUserId());
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

    // TODO: replace with however your auth actually resolves the current
    // user once it's wired up - same assumption as PharmacyController.
    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(claim, out var userId))
            return userId;

        throw new UnauthorizedAccessException("No authenticated user found.");
    }
}