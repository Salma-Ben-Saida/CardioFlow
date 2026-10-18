using CathLab.Application.DTOs.Archive;
using CathLab.Application.Services;
using CathLab.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly InventoryService _inventory;
    private readonly AuditService _audit;
    private readonly PatientLifecycleService _lifecycle;
    private readonly PacsOverviewService _pacsOverview;

    public AdminController(InventoryService inventory, AuditService audit, PatientLifecycleService lifecycle, PacsOverviewService pacsOverview)
    {
        _inventory = inventory;
        _audit = audit;
        _lifecycle = lifecycle;
        _pacsOverview = pacsOverview;
    }

    /// <summary>GET /api/admin/movements?page=1&pageSize=50</summary>
    [HttpGet("movements")]
    public async Task<IActionResult> GetMovements([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _inventory.GetStockMovementsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>GET /api/admin/audits?page=1&pageSize=50</summary>
    [HttpGet("audits")]
    public async Task<IActionResult> GetAudits([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _audit.GetAuditsAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>GET /api/admin/logs?page=1&pageSize=50</summary>
    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var result = await _audit.GetActivityLogsAsync(page, pageSize);
        return Ok(result);
    }
        // ───────────── Archive / restore / purge ─────────────

    /// <summary>GET /api/admin/archive: archived patients and studies</summary>
    [HttpGet("archive")]
    public async Task<IActionResult> GetArchive() => Ok(await _lifecycle.GetArchiveAsync());
    
    /// <summary>GET /api/admin/pacs/studies: Orthanc studies with their state in CathLab</summary>
    [HttpGet("pacs/studies")]
    public async Task<IActionResult> GetPacsStudies() => Ok(await _pacsOverview.GetAsync());

    [HttpPost("studies/{id:guid}/archive")]
    public Task<IActionResult> ArchiveStudy(Guid id, [FromBody] ArchiveRequestDto dto) =>
        Lifecycle(() => _lifecycle.ArchiveStudyAsync(id, dto.Reason, User.GetUserId()), $"Étude {id} introuvable.");

    [HttpPost("studies/{id:guid}/restore")]
    public Task<IActionResult> RestoreStudy(Guid id) =>
        Lifecycle(() => _lifecycle.RestoreStudyAsync(id, User.GetUserId()), $"Étude {id} introuvable.");

    [HttpDelete("studies/{id:guid}/purge")]
    public Task<IActionResult> PurgeStudy(Guid id) =>
        Lifecycle(() => _lifecycle.PurgeStudyAsync(id, User.GetUserId()), $"Étude {id} introuvable.");

    [HttpPost("patients/{id:guid}/archive")]
    public Task<IActionResult> ArchivePatient(Guid id, [FromBody] ArchiveRequestDto dto) =>
        Lifecycle(() => _lifecycle.ArchivePatientAsync(id, dto.Reason, User.GetUserId()), $"Patient {id} introuvable.");

    [HttpPost("patients/{id:guid}/restore")]
    public Task<IActionResult> RestorePatient(Guid id) =>
        Lifecycle(() => _lifecycle.RestorePatientAsync(id, User.GetUserId()), $"Patient {id} introuvable.");

    [HttpDelete("patients/{id:guid}/purge")]
    public Task<IActionResult> PurgePatient(Guid id) =>
        Lifecycle(() => _lifecycle.PurgePatientAsync(id, User.GetUserId()), $"Patient {id} introuvable.");

    // 204 = done, 404 = not found, 400 = invalid input, 409 = business rule (already archived, confirmed report…)
    private async Task<IActionResult> Lifecycle(Func<Task<bool>> action, string notFoundMessage)
    {
        try
        {
            return await action() ? NoContent() : NotFound(new { message = notFoundMessage });
        }
        catch (ArgumentException ex)         { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}