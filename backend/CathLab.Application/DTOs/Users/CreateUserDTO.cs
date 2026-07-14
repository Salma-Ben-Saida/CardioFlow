namespace CathLab.Application.Dtos;

public class CreateUserDto
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FullName { get; set; }
    public required UserRole Role { get; set; }
}
