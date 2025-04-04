using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;
using UserService.DTOs.Auth;
using UserService.Repositories;
using UserService.Constants;

namespace UserService.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAuthRepository authRepository,
        IConfiguration configuration,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Succeeded, string? Token, IEnumerable<string> Errors)> RegisterAsync(RegisterDto dto)
    {
        try
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName
            };

            var (succeeded, errors) = await _authRepository.CreateUserAsync(user, dto.Password);
            if (!succeeded)
            {
                return (false, null, errors);
            }

            // Add default User role
            await _authRepository.AddToRoleAsync(user, RoleConstants.User);

            // Get all user roles and generate token
            var userRoles = await _authRepository.GetUserRolesAsync(user);
            var token = await GenerateJwtTokenAsync(user, userRoles);
            
            return (true, token, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for user {Email}", dto.Email);
            throw;
        }
    }

    public async Task<(bool Succeeded, string? Token, IEnumerable<string> Errors)> LoginAsync(LoginDto dto)
    {
        try
        {
            var user = await _authRepository.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                return (false, null, new[] { "Invalid email or password" });
            }

            var isValidPassword = await _authRepository.ValidatePasswordAsync(user, dto.Password);
            if (!isValidPassword)
            {
                return (false, null, new[] { "Invalid email or password" });
            }

            // Get all user roles and generate token
            var userRoles = await _authRepository.GetUserRolesAsync(user);
            var token = await GenerateJwtTokenAsync(user, userRoles);
            
            return (true, token, Array.Empty<string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Email}", dto.Email);
            throw;
        }
    }

    private async Task<string> GenerateJwtTokenAsync(ApplicationUser user, IList<string> roles)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email!),
        new(ClaimTypes.Name, user.FullName)
    };

    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddDays(1);

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: expires,
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
}