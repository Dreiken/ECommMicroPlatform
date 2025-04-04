using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UserService.Data;
using Microsoft.IdentityModel.Tokens;
using UserService.Models;
using DotNetEnv;
using UserService.Services;
using UserService.Repositories;
using UserService.Extensions;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

static string EnsureValidJwtKey(string key)
{
    if (string.IsNullOrEmpty(key) || key.Length < 32)
    {
        throw new InvalidOperationException("JWT secret key must be at least 32 characters long");
    }
    return key;
}


// Get database password from env variables
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
var connectionString = builder.Configuration.GetConnectionString("UserDb")
    .Replace("__DB_PASSWORD__", Environment.GetEnvironmentVariable("DB_PASSWORD"));
    
var jwtSecret = EnsureValidJwtKey(builder.Configuration["Jwt:Secret"]!);
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

// Configure SQL Server
builder.Services.AddDbContext<UserContext>(options => 
    options.UseSqlServer(connectionString));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
})
.AddEntityFrameworkStores<UserContext>()
.AddDefaultTokenProviders();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure JWT
builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)
        ),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
        RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            var claims = context.Principal?.Claims.Select(c => new { c.Type, c.Value });
            Console.WriteLine($"UserService - Token validated. Claims: {System.Text.Json.JsonSerializer.Serialize(claims)}");
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"UserService - Authentication failed: {context.Exception.Message}");
            if (context.Exception.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {context.Exception.InnerException.Message}");
            }
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Headers["Authorization"].ToString()?.Replace("Bearer ", "");
            Console.WriteLine($"UserService - Received token: {accessToken}");
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

//Migration and seeding
try
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<UserContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    logger.LogInformation("Starting database migration...");
    await context.Database.MigrateAsync();
    logger.LogInformation("Database migration completed");

    await app.Services.InitializeDatabaseAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database");
    throw;
}

//test middleware
app.Use(async (context, next) =>
{
    Console.WriteLine($"UserService - Request path: {context.Request.Path}");
    Console.WriteLine($"UserService - Authorization header: {context.Request.Headers["Authorization"]}");

    if (context.User?.Identity?.IsAuthenticated == true)
    {
        var claims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        Console.WriteLine($"UserService - Authenticated User Claims: {System.Text.Json.JsonSerializer.Serialize(claims)}");
    }
    else
    {
        Console.WriteLine("UserService - User is not authenticated");
    }

    await next();
});

app.Run();
