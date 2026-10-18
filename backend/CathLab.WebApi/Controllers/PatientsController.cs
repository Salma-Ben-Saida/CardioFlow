using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/patients")]
[Authorize(Roles = "Admin,Medecin,Secretaire")]
public class PatientsController : ControllerBase
{
    private readonly PatientService _patientService;

    public PatientsController(PatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>
    /// GET /api/patients?search=ali&sex=M&status=complete&page=1&pageSize=20
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] string? search,
        [FromQuery] string? sex,
        [FromQuery] string? status,   // "complete" | "incomplete" | ""
        [FromQuery] string? sortBy = "updatedAt", 
        [FromQuery] string? sortDir = "desc",
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var result = await _patientService.GetListAsync(search, sex, status, sortBy, sortDir, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/patients/{id}
    /// Fetches extensive, single-patient master details.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        var patient = await _patientService.GetPatientDetailAsync(id);
        
        if (patient == null)
            return NotFound(new { message = $"Patient with ID {id} was not found." });

        return Ok(patient);
    }

    /// <summary>
    /// GET /api/patients/{id}/studies
    /// Returns a targeted sub-list of cath lab cardiac records assigned to this individual.
    /// </summary>
    [HttpGet("{id:guid}/studies")]
    public async Task<IActionResult> GetStudies(Guid id)
    {
        var studies = await _patientService.GetPatientStudiesAsync(id);
        return Ok(studies);
    }

    /// <summary>
    /// GET /api/patients/{id}/risk-profile
    /// Pulls specific historical medical vulnerability indexes for the chosen identifier.
    /// </summary>
    [HttpGet("{id:guid}/risk-profile")]
    public async Task<IActionResult> GetRiskProfile(Guid id)
    {
        var riskProfile = await _patientService.GetPatientRiskProfileAsync(id);
        return Ok(riskProfile); // Returns null or 200 OK containing payload data 
    }
}
