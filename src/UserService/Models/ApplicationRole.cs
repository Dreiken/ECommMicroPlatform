using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description {get; set;}
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}