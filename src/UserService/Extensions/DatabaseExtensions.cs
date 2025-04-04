using Microsoft.EntityFrameworkCore;
using UserService.Data;
using UserService.Models;
using Microsoft.AspNetCore.Identity;

namespace UserService.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<UserContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<UserContext>>();

        try
        {
            logger.LogInformation("Attempting to migrate database...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrated successfully");

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new ApplicationRole 
                { 
                    Name = "Admin",
                    Description = "Administrator role with full access"
                });
            }

            if (!await roleManager.RoleExistsAsync("Boss"))
            {
                await roleManager.CreateAsync(new ApplicationRole 
                { 
                    Name = "Boss",
                    Description = "Boss role with department access"
                });
            }

            if (!await roleManager.RoleExistsAsync("Employee"))
            {
                await roleManager.CreateAsync(new ApplicationRole 
                { 
                    Name = "Employee",
                    Description = "Standard employee role"
                });
            }
            if (!await roleManager.RoleExistsAsync("User"))
            {
                await roleManager.CreateAsync(new ApplicationRole 
                { 
                    Name = "User",
                    Description = "Standard user role"
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }
}