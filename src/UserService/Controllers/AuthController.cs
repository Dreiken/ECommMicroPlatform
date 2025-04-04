using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs.Auth;
using UserService.Models;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        try
        {
            var (succeeded, token, errors) = await _authService.RegisterAsync(dto);
            
            if (!succeeded)
            {
                return BadRequest(new { Errors = errors });
            }

            return Ok(new AuthResponseDto 
            { 
                Token = token!, 
                Email = dto.Email,
                Roles = new[] { Constants.RoleConstants.User }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user {Email}", dto.Email);
            return StatusCode(500, "An error occurred during registration");
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        try
        {
            var (succeeded, token, errors) = await _authService.LoginAsync(dto);
            
            if (!succeeded)
            {
                return BadRequest(new { Errors = errors });
            }

            return Ok(new AuthResponseDto 
            { 
                Token = token!, 
                Email = dto.Email,
                Roles = new[] { Constants.RoleConstants.User }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user {Email}", dto.Email);
            return StatusCode(500, "An error occurred during login");
        }
    }

    [HttpGet("test-token")]
    public IActionResult TestToken()
    {
        var userId = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (userId == null)
        {
            return Unauthorized("User ID not found in token");
        }

        var user = new ApplicationUser
        {
            Id = Guid.Parse(userId),
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            FullName = User.FindFirst(ClaimTypes.Name)?.Value
        };

        var roles = User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();

        return Ok(new
        {
            User = user,
            Roles = roles
        });
    }
}