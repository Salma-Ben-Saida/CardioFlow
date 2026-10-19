using CathLab.Application.DTOs.Pacs;
using CathLab.Application.Interfaces;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pacs")]
[Authorize(Roles = "Admin,Secretaire, Medecin")]
public class PacsController : ControllerBase
{
    private readonly IPacsReader _pacs;
    private readonly StudyIngestionService _ingestion;
    private readonly PacsOverviewService _overview;


    public PacsController(IPacsReader pacs, StudyIngestionService ingestion, PacsOverviewService overview)
    {
        _pacs = pacs;
        _ingestion = ingestion;
        _overview = overview;
    }

    [HttpGet("studies")]
    public async Task<IActionResult> GetStudies()
    {
        var studies = await _pacs.GetPendingStudiesAsync();
        return Ok(studies);
    }

    // Orthanc studies + their state in CathLab (read-only: Admin, Secretaire, Medecin)
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview() => Ok(await _overview.GetAsync());

    // Ingest one study by accession number
    [HttpPost("ingest/{accessionNumber}")]
public async Task<IActionResult> IngestOne(string accessionNumber, [FromBody] PatientOverrideDto? overrideData)
{
    var result = await _ingestion.IngestStudyAsync(accessionNumber, overrideData);
    if (!result.Succeeded)
        return BadRequest(new { error = result.Error });
    return Ok(result);
}

    // Ingest all studies from last 30 days
    [HttpPost("ingest/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> IngestAll()
    {
        var results = await _ingestion.IngestAllPendingAsync();
        return Ok(results);
    }
}