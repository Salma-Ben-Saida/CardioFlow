namespace CathLab.Api.Controllers;

using CathLab.Application.Dtos;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuditService _audit;

    public AuthController(UserService userService, AuditService audit)
    {
        _userService = userService;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();

        try
        {
            var result = await _userService.LoginAsync(dto);
            await _audit.LogActivityAsync(result.UserId, dto.Username, "LoginSuccess", ip, ua);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            await _audit.LogActivityAsync(null, dto.Username, "LoginFailed", ip, ua);
            return Unauthorized(new { message = ex.Message });
        }
    }
}