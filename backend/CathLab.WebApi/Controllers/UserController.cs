namespace CathLab.Api.Controllers;

using System.Security.Claims;
using CathLab.Application.Dtos;
using CathLab.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuditService _audit;

    public UserController(UserService userService, AuditService audit)
    {
        _userService = userService;
        _audit = audit;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        try
        {
            // Appelle le service pour valider, hacher et enregistrer l'utilisateur
            var createdUser = await _userService.CreateUserAsync(dto);

            await _audit.LogActionAsync(GetCurrentUserId(), "Create", "AppUser", createdUser.Id,
                $"Created user {createdUser.Username} ({createdUser.Role})");

            // Retourne un statut 201 Created avec l'objet sans le mot de passe pour des raisons de sécurité
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, new
                {
                    createdUser.Id,
                    createdUser.Username,
                    createdUser.FullName,
                    createdUser.Role,
                    createdUser.IsActive,
                    createdUser.CreatedAt
                });
        }
        catch (InvalidOperationException ex)
        {
            // Gère le cas où le nom d'utilisateur existe déjà
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception)
        {
            // Gère les erreurs système imprévues
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new { message = "Une erreur interne est survenue lors de la création de l'utilisateur." });
        }
    }
[HttpGet]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<UserDto>))]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
{
    var users = await _userService.GetUsersAsync(page, pageSize);
    return Ok(users);
}
[HttpGet("{id:guid}")]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetUserById(Guid id)
{
    try
    {
        var user = await _userService.GetUserByIdAsync(id);
        return Ok(user);
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(new { message = ex.Message });
    }
}
private Guid GetCurrentUserId()
{
    var userPrincipal = User;

    if (userPrincipal?.Identity?.IsAuthenticated != true)
    {
        throw new UnauthorizedAccessException("No authenticated user found.");
    }

    var userIdClaim = userPrincipal.FindFirst("nameid")?.Value
                      ?? userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? userPrincipal.FindFirst("sub")?.Value;

    if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
    {
        throw new UnauthorizedAccessException("User identification token is invalid.");
    }

    return userId;
}
}
