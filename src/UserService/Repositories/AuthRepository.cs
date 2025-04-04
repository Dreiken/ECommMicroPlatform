using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;

namespace UserService.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserContext _context;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(
        UserManager<ApplicationUser> userManager,
        UserContext context,
        ILogger<AuthRepository> logger)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(
        ApplicationUser user, string password)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to create user {Email}", user.Email);
            throw;
        }
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string email)
    {
        try
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.NormalizedEmail == email.ToUpper());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find user by email {Email}", email);
            throw;
        }
    }

    public async Task<bool> ValidatePasswordAsync(ApplicationUser user, string password)
    {
        try
        {
            var passwordHash = _userManager.PasswordHasher.HashPassword(user, password);
            var result = _userManager.PasswordHasher.VerifyHashedPassword(
                user, 
                user.PasswordHash!, 
                password
            );
            return result == PasswordVerificationResult.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate password for user {Email}", user.Email);
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
    {
        try
        {
            var roles = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Join(
                    _context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => r.Name!
                )
                .ToListAsync();

            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get roles for user {Email}", user.Email);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(
        ApplicationUser user, string role)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to add user {Email} to role {Role}", user.Email, role);
            throw;
        }
    }
}