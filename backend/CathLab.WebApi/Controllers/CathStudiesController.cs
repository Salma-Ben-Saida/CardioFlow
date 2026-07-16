using CathLab.Application.DTOs.Patients;
using CathLab.Application.Interfaces;
using CathLab.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CathLab.WebApi.Controllers;

[ApiController]
[Route("api/cathstudies")]
[Authorize(Roles = "Admin,Medecin")]
public class CathStudiesController : ControllerBase
{
    private readonly ICathStudyRepository _cathStudies;

    public CathStudiesController(ICathStudyRepository cathStudies)
    {
        _cathStudies = cathStudies;
    }

    /// <summary>
    /// GET /api/cathstudies/{id} - lets the segmentation UI resolve which
    /// patient a study belongs to when it only has cathStudyId in the URL.
    /// </summary>
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
}