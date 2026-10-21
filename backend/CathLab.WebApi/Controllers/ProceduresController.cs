using System.Security.Claims;
using CathLab.Application.DTOs.Procedures;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/procedures")]
[Authorize(Roles = "Admin,Medecin, Secretaire")]
public class ProceduresController : ControllerBase
{
    private readonly ProcedureService _procedures;
    private readonly InventoryService _inventory;
    private readonly AuditService _audit;


    public ProceduresController(ProcedureService procedures, InventoryService inventory, AuditService audit)
    {
        _procedures = procedures;
        _inventory = inventory;
        _audit = audit;
    }

    /// <summary>
    /// POST /api/procedures/lesions - placing a Lesion marker in the
    /// segmentation UI. Creates the ProcedureReport on first use for
    /// this CathStudy if one doesn't exist yet.
    /// </summary>
    [HttpPost("lesions")]
    [Authorize(Roles = "Admin,Medecin")]
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
    [Authorize(Roles = "Admin,Medecin")]
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
    [Authorize(Roles = "Admin,Medecin")]

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
[Authorize(Roles = "Admin,Medecin")]
public async Task<IActionResult> ConfirmReport(Guid reportId)
{
    try
    {
        await _procedures.ConfirmReportAsync(reportId, GetCurrentUserId());
        await _audit.LogActionAsync(GetCurrentUserId(), "Confirm", "ProcedureReport", reportId, null);

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
/// <summary>
/// GET /api/procedures/reports/study/{cathStudyId} - hydrates the
/// coronary mapping UI with everything already recorded for this study.
/// Returns 204 (not 404) when no report exists yet - "nothing recorded"
/// is a normal state, not an error.
/// </summary>
[HttpGet("reports/study/{cathStudyId:guid}")]
public async Task<IActionResult> GetReportByCathStudy(Guid cathStudyId)
{
    var detail = await _procedures.GetReportDetailByCathStudyIdAsync(cathStudyId);
    return detail is null ? NoContent() : Ok(detail);
}

/// <summary>
/// PUT /api/procedures/reports/{reportId} - sets narrative fields
/// (Approach, CatheterUsed, Conclusion...) required before confirmation.
/// </summary>
[HttpPut("reports/{reportId:guid}")]
[Authorize(Roles = "Admin,Medecin")]
public async Task<IActionResult> UpdateReportNarrative(Guid reportId, [FromBody] UpdateReportNarrativeDto dto)
{
    try
    {
        await _procedures.UpdateReportNarrativeAsync(reportId, dto);
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { error = ex.Message });
    }
}

/// <summary>DELETE /api/procedures/lesions/{lesionId} - removes a marker entirely.</summary>
[HttpDelete("lesions/{lesionId:guid}")]
[Authorize(Roles = "Admin,Medecin")]
public async Task<IActionResult> DeleteLesion(Guid lesionId)
{
    try
    {
        await _procedures.DeleteLesionAsync(lesionId);
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { error = ex.Message });
    }
}

/// <summary>DELETE /api/procedures/treatments/{treatmentId} - removes one stent/balloon.</summary>
[HttpDelete("treatments/{treatmentId:guid}")]
[Authorize(Roles = "Admin,Medecin")]

public async Task<IActionResult> DeleteTreatment(Guid treatmentId)
{
    try
    {
        await _procedures.DeleteTreatmentAsync(treatmentId);
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { error = ex.Message });
    }
}

/// <summary>
/// GET /api/procedures/reports/{reportId}/pdf - streams the generated PDF.
/// Only available after confirmation, since PdfStoragePath is set at that point.
/// </summary>
[HttpGet("reports/{reportId:guid}/pdf")]
public async Task<IActionResult> DownloadReportPdf(Guid reportId)
{
    var report = await _procedures.GetReportByIdAsync(reportId);
    if (report is null) return NotFound();
    if (!report.IsConfirmed || string.IsNullOrEmpty(report.PdfStoragePath))
        return Conflict(new { error = "Le rapport doit être confirmé avant de pouvoir être téléchargé en PDF." });

    var bytes = await System.IO.File.ReadAllBytesAsync(report.PdfStoragePath);
    return File(bytes, "application/pdf", $"rapport-{reportId}.pdf");
}

}