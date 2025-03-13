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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();


// Get database password from env variables
Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
var connectionString = builder.Configuration.GetConnectionString("UserDb")
    .Replace("__DB_PASSWORD__", Environment.GetEnvironmentVariable("DB_PASSWORD"));
    
var jwtSecret = builder.Configuration["Jwt:Secret"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

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

// Configure JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();
app.MapHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

app.Run();
