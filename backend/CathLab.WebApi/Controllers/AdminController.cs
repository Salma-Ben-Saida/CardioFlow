using CathLab.Application.Services;
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

    public AdminController(InventoryService inventory, AuditService audit)
    {
        _inventory = inventory;
        _audit = audit;
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
}