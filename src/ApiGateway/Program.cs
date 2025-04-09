using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.IdentityModel.Logging;
using Serilog;
using ApiGateway.Services;
using ApiGateway.Handlers;
using ApiGateway.Middleware;
using Ocelot.Provider.Polly;

IdentityModelEventSource.ShowPII = true;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://*:80");

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.WithCorrelationId()
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddScoped<ICorrelationIdGenerator, CorrelationIdGenerator>();
builder.Services.AddTransient<CorrelationIdMiddleware>();

// Validate JWT configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? 
    throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? 
    throw new InvalidOperationException("JWT issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? 
    throw new InvalidOperationException("JWT audience not configured");

if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Length < 32)
{
    throw new InvalidOperationException("JWT secret must be at least 32 characters long");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Load Ocelot configuration from ocelot.json
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot()
    .AddDelegatingHandler<CircuitBreakerLoggingHandler>()
    .AddPolly();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

// Use Ocelot middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

await app.UseOcelot();

app.MapGet("/health", () => Results.Ok("Healthy"));

app.Run();