using UserService.Models;

namespace UserService.Repositories;

public interface IAuthRepository
{
    Task<(bool Succeeded, IEnumerable<string> Errors)> CreateUserAsync(ApplicationUser user, string password);
    
    Task<ApplicationUser?> FindByEmailAsync(string email);
    
    Task<bool> ValidatePasswordAsync(ApplicationUser user, string password);
    
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    
    Task<(bool Succeeded, IEnumerable<string> Errors)> AddToRoleAsync(ApplicationUser user, string role);
}