namespace CathLab.Application.Dtos;

using CathLab.Domain.Entities;

public record UserDto(
    Guid Id,
    string Username,
    string FullName,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt
);
