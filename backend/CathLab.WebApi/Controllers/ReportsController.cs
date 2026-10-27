using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CathLab.WebApi.Extensions;

namespace CathLab.WebApi.Controllers;

/// <summary>
/// Read-only report listing/search. Kept separate from ProceduresController,
/// which owns the write-side workflow (lesions, treatments, confirming a
/// specific report). This controller answers "which reports exist" rather
/// than "act on this one report".
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,Medecin")]
public class ReportsController : ControllerBase
{
    private readonly ProcedureService _procedures;

    public ReportsController(ProcedureService procedures)
    {
        _procedures = procedures;
    }

    /// <summary>
    /// GET /api/reports - reports for the doctor's own patients.
    /// A Medecin always gets their own list. An Admin may pass ?doctorId=
    /// to view a specific doctor's list, or omit it to see every report
    /// in the system.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetReports([FromQuery] Guid? doctorId)
    {
        var isAdmin = User.IsInRole("Admin");

        if (!isAdmin)
        {
            // Medecin can only ever see their own list - a client-supplied
            // doctorId is ignored for anyone but an Admin.
            var ownReports = await _procedures.GetReportsForDoctorAsync(User.GetUserId());
            return Ok(ownReports);
        }

        if (doctorId.HasValue)
        {
            var reportsForDoctor = await _procedures.GetReportsForDoctorAsync(doctorId.Value);
            return Ok(reportsForDoctor);
        }

        var allReports = await _procedures.GetAllReportsAsync();
        return Ok(allReports);
    }

    /// <summary>
    /// GET /api/reports/patient/{patientId} - full report history for a
    /// patient, from any doctor who has treated them. A Medecin can only
    /// access this if they have themselves performed at least one study
    /// on this patient; Admins can access any patient.
    /// </summary>
    [HttpGet("patient/{patientId:guid}")]
    public async Task<IActionResult> GetReportsForPatient(Guid patientId)
    {
        try
        {
            var isAdmin = User.IsInRole("Admin");
            var reports = await _procedures.GetReportsForPatientAsync(patientId, User.GetUserId(), isAdmin);
            return Ok(reports);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}