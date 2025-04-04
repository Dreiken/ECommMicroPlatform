using UserService.DTOs.Auth;
using UserService.Models;

namespace UserService.Services;

public interface IAuthService
{
    Task<(bool Succeeded, string? Token, IEnumerable<string> Errors)> RegisterAsync(RegisterDto registerDto);
    Task<(bool Succeeded, string? Token, IEnumerable<string> Errors)> LoginAsync(LoginDto loginDto);
}