using CathLab.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/pacs")]
public class PacsController : ControllerBase
{
    private readonly IPacsReader _pacs;
    public PacsController(IPacsReader pacs) => _pacs = pacs;

    [HttpGet("studies")]
    public async Task<IActionResult> GetStudies()
    {
        var studies = await _pacs.GetPendingStudiesAsync(DateTime.UtcNow.AddDays(-30));
        return Ok(studies);
    }
}