namespace CathLab.Application.Dtos;

public class UpdateUserDto
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
}
