using UserService.Models;

namespace UserService.Repositories;
public interface IRoleRepository
{
    Task<ApplicationRole?> GetByIdAsync(Guid id);
    Task<ApplicationRole?> GetByNameAsync(string normalizedName);
    Task<IReadOnlyList<ApplicationRole>> GetAllAsync();
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateAsync(ApplicationRole role);
    Task<(bool Succeeded, IEnumerable<string> Errors)> UpdateAsync(ApplicationRole role);
    Task<(bool Succeeded, IEnumerable<string> Errors)> DeleteAsync(Guid id);
    Task<(bool Succeeded, IEnumerable<string> Errors)> AssignUserToRoleAsync(Guid userId, string roleName);
    Task<(bool Succeeded, IEnumerable<string> Errors)> RemoveUserFromRoleAsync(Guid userId, string roleName);
    Task<IList<string>> GetUserRolesAsync(Guid userId);
    Task<bool> ExistsAsync(string normalizedName);
}