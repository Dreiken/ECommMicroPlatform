using Microsoft.AspNetCore.Identity;
using UserService.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace UserService.Data;
public class UserContext : IdentityDbContext<
    ApplicationUser,
    ApplicationRole,
    Guid,
    ApplicationUserClaim,
    ApplicationUserRole,
    ApplicationUserLogin,
    ApplicationRoleClaim,
    ApplicationUserToken
    
>
{
    public UserContext(DbContextOptions<UserContext> options) 
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<ApplicationUserClaim>().ToTable("UserClaims");
        builder.Entity<ApplicationUserLogin>().ToTable("UserLogins");
        builder.Entity<ApplicationUserToken>().ToTable("UserTokens");
        builder.Entity<ApplicationRoleClaim>().ToTable("RoleClaims");
        builder.Entity<ApplicationUserRole>().ToTable("UserRoles");
    }
}