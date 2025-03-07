using Microsoft.AspNetCore.Identity;

namespace UserService.Models;

public class ApplicationRole : IdentityRole<Guid>
{
    public string? Description {get; set;}
}