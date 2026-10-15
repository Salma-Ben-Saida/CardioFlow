namespace CathLab.Application.Services;

using CathLab.Application.Dtos;
using CathLab.Domain.Entities;
using CathLab.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

public record LoginDto(string Username, string Password);
public record AuthResponseDto(string Token, string Username, string FullName, Guid UserId);

public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;


    public UserService(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    public async Task<AppUser> CreateUserAsync(CreateUserDto dto)
    {
        // 1. Validation de l'existence de l'utilisateur
        var existingUser = await _userRepository.GetByUsernameAsync(dto.Username);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"Username '{dto.Username}' is already taken.");
        }

        // 2. Hachage sécurisé avec l'implémentation locale (Pbkdf2 natif .NET)
        string passwordHash = CryptoUtils.HashPassword(dto.Password);

        // 3. Mapping vers l'entité du domaine
        var newUser = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            PasswordHash = passwordHash,
            FullName = dto.FullName,
            Role = dto.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // 4. Sauvegarde en base de données
        await _userRepository.AddAsync(newUser);
        await _userRepository.SaveChangesAsync();

        return newUser;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        // 1. Rechercher l'utilisateur
        var user = await _userRepository.GetByUsernameAsync(dto.Username);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Identifiants invalides.");
        }

        // 2. Vérifier le mot de passe
        var isPasswordValid = CryptoUtils.VerifyPassword(dto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Identifiants invalides.");
        }

        // 3. Générer le jeton JWT
        string token = GenerateJwtToken(user);

        return new AuthResponseDto(token, user.Username, user.FullName, user.Id);
    }

    private string GenerateJwtToken(AppUser user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Secret"]!));

        // Keep a single canonical JWT claim set so both login and logout use the same contract.
        var claims = new[]
        {
            new Claim("nameid", user.Id.ToString()),
            new Claim("name", user.Username),
            new Claim("role", user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["DurationInMinutes"]!)),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        
        return tokenHandler.WriteToken(securityToken);
    }

    // Classe utilitaire de cryptographie mise à jour
    private static class CryptoUtils
    {
        private const int KeySize = 32;       
        private const int Iterations = 350000; 
        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA512;

        internal static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithm, KeySize);
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }

        internal static bool VerifyPassword(string password, string storedHash)
        {
            var parts = storedHash.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] hash = Convert.FromBase64String(parts[1]);

            byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithm, KeySize);

            // Comparaison à temps constant pour éviter les attaques temporelles
            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
    }
    public async Task<UserDto> UpdateUserAsync(Guid id, UpdateUserDto dto)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found.");
        }

        var updatedUsername = dto.Username?.Trim();
        if (!string.IsNullOrWhiteSpace(updatedUsername) && updatedUsername != user.Username)
        {
            var existingUser = await _userRepository.GetByUsernameAsync(updatedUsername);
            if (existingUser is not null && existingUser.Id != user.Id)
            {
                throw new InvalidOperationException($"Username '{updatedUsername}' is already taken.");
            }

            user.Username = updatedUsername;
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            user.FullName = dto.FullName.Trim();
        }

        if (dto.Role.HasValue)
        {
            user.Role = dto.Role.Value;
        }

        if (dto.IsActive.HasValue)
        {
            user.IsActive = dto.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = CryptoUtils.HashPassword(dto.Password);
        }

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.IsActive,
            user.CreatedAt
        );
    }

    public async Task DeleteUserAsync(Guid id, Guid currentUserId)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found.");
        }

        if (user.Id == currentUserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        await _userRepository.DeleteAsync(user);
        await _userRepository.SaveChangesAsync();
    }

    public async Task<List<UserDto>> GetUsersAsync(int page = 1, int pageSize = 20)
{
    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;

    var users = await _userRepository.GetAllAsync(page, pageSize);

    return users.Select(user => new UserDto(
        user.Id,
        user.Username,
        user.FullName,
        user.Role,
        user.IsActive,
        user.CreatedAt
    )).ToList();
}

    public async Task<UserDto> GetUserByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user is null)
        {
            throw new KeyNotFoundException($"User with ID '{id}' not found.");
        }

        return new UserDto(
            user.Id,
            user.Username,
            user.FullName,
            user.Role,
            user.IsActive,
            user.CreatedAt
        );
    }
}
