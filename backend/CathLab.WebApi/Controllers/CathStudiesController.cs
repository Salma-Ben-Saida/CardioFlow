using CathLab.Application.DTOs.Patients;
using CathLab.Application.Interfaces;
using CathLab.Application.Services;
using CathLab.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/cathstudies")]
[Authorize(Roles = "Admin,Medecin,Secretaire")]
public class CathStudiesController : ControllerBase
{
    private readonly ICathStudyRepository _cathStudies;
    private readonly StudySchedulingService _scheduling;
    private readonly AuditService _audit;

    public CathStudiesController(ICathStudyRepository cathStudies, StudySchedulingService scheduling, AuditService audit)
    {
        _cathStudies = cathStudies;
        _scheduling = scheduling;
        _audit = audit;
    }

    /// <summary>GET /api/cathstudies - every ingested study with its status and planning.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _scheduling.GetAllAsync());

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var study = await _cathStudies.FindByIdAsync(id);
        if (study is null)
            return NotFound(new { message = $"CathStudy with ID {id} was not found." });

        return Ok(new CathStudyDto
        {
            Id = study.Id,
            PatientId = study.PatientId,
            AccessionNumber = study.AccessionNumber,
            StudyInstanceUid = study.StudyInstanceUid,
            StudyDate = study.StudyDate,
            Status = study.Status.ToString(),
            OperatorName = study.Operator?.FullName,
            CreatedAt = study.CreatedAt
        });
    }

    /// <summary>PUT /api/cathstudies/{id}/schedule - set (or change) the intervention date/time.</summary>
    [HttpPut("{id:guid}/schedule")]
    public async Task<IActionResult> Schedule(Guid id, [FromBody] ScheduleStudyDto dto)
    {
        try
        {
            await _scheduling.ScheduleAsync(id, dto.ScheduledAt);
            await _audit.LogActionAsync(User.GetUserId(), "Schedule", "CathStudy", id,
                $"Intervention planifiée le {dto.ScheduledAt.ToUniversalTime():yyyy-MM-dd HH:mm} UTC");
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    /// <summary>POST /api/cathstudies/{id}/cancel - only while Pending or Scheduled.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        try
        {
            await _scheduling.CancelAsync(id);
            await _audit.LogActionAsync(User.GetUserId(), "Cancel", "CathStudy", id, "Étude annulée");
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}