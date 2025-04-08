using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserService.Models;

namespace UserService.Repositories;

/// <summary>
/// Implements role management operations using ASP.NET Core Identity
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<RoleRepository> _logger;

    public RoleRepository(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        ILogger<RoleRepository> logger)
    {
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ApplicationRole?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _roleManager.FindByIdAsync(id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve role with ID {RoleId}", id);
            throw;
        }
    }

    public async Task<ApplicationRole?> GetByNameAsync(string normalizedName)
    {
        try
        {
            return await _roleManager.FindByNameAsync(normalizedName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve role with name {RoleName}", normalizedName);
            throw;
        }
    }

    public async Task<IReadOnlyList<ApplicationRole>> GetAllAsync()
    {
        try
        {
            return await _roleManager.Roles.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all roles");
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationRole role)
    {
        try
        {
            var result = await _roleManager.CreateAsync(role);
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role {RoleName}", role.Name);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateAsync(ApplicationRole role)
    {
        try
        {
            var result = await _roleManager.UpdateAsync(role);
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role {RoleName}", role.Name);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(Guid id)
    {
        try
        {
            var role = await GetByIdAsync(id);
            if (role == null)
            {
                return (false, new[] { "Role not found" });
            }

            var result = await _roleManager.DeleteAsync(role);
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete role with ID {RoleId}", id);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AssignUserToRoleAsync(
        Guid userId, string roleName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> RemoveUserFromRoleAsync(
        Guid userId, string roleName)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return (false, new[] { "User not found" });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            return (result.Succeeded, result.Errors.Select(e => e.Description));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove role {RoleName} from user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return Array.Empty<string>();
            }

            return await _userManager.GetRolesAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve roles for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string normalizedName)
    {
        try
        {
            return await _roleManager.RoleExistsAsync(normalizedName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check existence of role {RoleName}", normalizedName);
            throw;
        }
    }
}