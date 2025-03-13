using UserService.DTOs.Role;

namespace UserService.Services;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(Guid id);
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateRoleAsync(CreateRoleDto dto);
    Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateRoleAsync(Guid id, UpdateRoleDto dto);
    Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteRoleAsync(Guid id);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AssignRoleToUserAsync(Guid userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(Guid userId);
}