using Microsoft.Extensions.Logging;
using UserService.DTOs.Role;
using UserService.Models;
using UserService.Repositories;

namespace UserService.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly ILogger<RoleService> _logger;

    public RoleService(IRoleRepository roleRepository, ILogger<RoleService> logger)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync()
    {
        try
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(MapToDto).ToList().AsReadOnly();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all roles");
            throw;
        }
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid id)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            return role != null ? MapToDto(role) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve role with ID {RoleId}", id);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> CreateRoleAsync(CreateRoleDto dto)
    {
        try
        {
            var role = new ApplicationRole
            {
                Name = dto.Name,
                NormalizedName = dto.Name.ToUpperInvariant(),
                Description = dto.Description
            };

            return await _roleRepository.CreateAsync(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create role {RoleName}", dto.Name);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateRoleAsync(Guid id, UpdateRoleDto dto)
    {
        try
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null)
            {
                return (false, new[] { "Role not found" });
            }

            role.Description = dto.Description;
            return await _roleRepository.UpdateAsync(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update role with ID {RoleId}", id);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteRoleAsync(Guid id)
    {
        try
        {
            return await _roleRepository.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete role with ID {RoleId}", id);
            throw;
        }
    }

    public async Task<(bool Succeeded, IEnumerable<string> Errors)> AssignRoleToUserAsync(
        Guid userId, string roleName)
    {
        try
        {
            return await _roleRepository.AssignUserToRoleAsync(userId, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to assign role {RoleName} to user {UserId}", roleName, userId);
            throw;
        }
    }

    public async Task<IList<string>> GetUserRolesAsync(Guid userId)
    {
        try
        {
            return await _roleRepository.GetUserRolesAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve roles for user {UserId}", userId);
            throw;
        }
    }

    private static RoleDto MapToDto(ApplicationRole role) => new()
    {
        Id = role.Id,
        Name = role.Name!,
        Description = role.Description
    };
}