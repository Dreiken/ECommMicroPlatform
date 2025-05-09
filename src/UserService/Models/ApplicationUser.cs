using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}