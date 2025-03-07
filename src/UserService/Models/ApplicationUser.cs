using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class AppilcationUser : IdentityUser<Guid>
{
    public string FullName {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
}