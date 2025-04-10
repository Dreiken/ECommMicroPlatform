using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using RabbitMQ.Client;
using System.Text;
using DotNetEnv;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OrderService.Enums;
using OrderService.Repositories;
using OrderService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Polly;

var builder = WebApplication.CreateBuilder(args);


// Get database password from env variables
var config = builder.Configuration;

Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"));
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var orderDbConnectionString = builder.Configuration.GetConnectionString("OrderDb")
    ?.Replace("__DB_PASSWORD__", dbPassword)
    ?? throw new InvalidOperationException("OrderDb connection string is not configured");

var rabbitMqUser = Environment.GetEnvironmentVariable("RABBITMQ_USER");
var rabbitMqPass = Environment.GetEnvironmentVariable("RABBITMQ_PASS");
if (!string.IsNullOrEmpty(rabbitMqUser))
{
    config["RabbitMQ:Username"] = rabbitMqUser;
}

if (!string.IsNullOrEmpty(rabbitMqPass))
{
    config["RabbitMQ:Password"] = rabbitMqPass;
}

// Add SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        orderDbConnectionString,
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null
            );
        }
    )
);

//Add RabbitMQ

builder.Services.AddSingleton<IConnection>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<Program>>();
        var factory = new ConnectionFactory
        {
            HostName = builder.Configuration["RabbitMQ:Host"],
            UserName = builder.Configuration["RabbitMQ:Username"],
            Password = builder.Configuration["RabbitMQ:Password"],
            RequestedHeartbeat = TimeSpan.FromSeconds(60),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            ConsumerDispatchConcurrency = 2
        };

        var retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetry(
                retryCount: 10,
                sleepDurationProvider: attemptNumber => 
                    TimeSpan.FromSeconds(Math.Pow(2, attemptNumber)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning(
                        exception,
                        "Failed to connect to RabbitMQ (Attempt {RetryCount}/10). Retrying in {TimeSpan}...",
                        retryCount,
                        timeSpan);
                });

        return retryPolicy.Execute(() =>
        {
            logger.LogInformation("Attempting to connect to RabbitMQ...");
            var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            if (connection.IsOpen)
            {
                logger.LogInformation("Successfully connected to RabbitMQ");
            }
            else
            {
                logger.LogWarning("Connection to RabbitMQ is not open");
            }
            return connection;
        });
    });

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? 
    throw new InvalidOperationException("JWT secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? 
    throw new InvalidOperationException("JWT issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? 
    throw new InvalidOperationException("JWT audience not configured");

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


// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: orderDbConnectionString,
        name: "sql-server",
        tags: new[] { "db", "sql", "sqlserver" })
    .AddCheck<RabbitMQHealthCheck>("rabbitmq");

// Add this class in the same file, after the app.Run() call:

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80); // Listen on port 80 inside container
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration.ToString()
            })
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, result);
    }
});

// Ensure database exists and apply migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var retryCount = 0;
    const int maxRetries = 3;

    while (retryCount < maxRetries)
    {
        try
        {
            logger.LogInformation("Attempting to ensure database exists and is up to date");
            var context = services.GetRequiredService<AppDbContext>();
            
            // Ensure database exists
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Database check completed successfully");
            break;
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogError(ex, "Error occurred while ensuring database exists (Attempt {RetryCount}/{MaxRetries})", 
                retryCount, maxRetries);
            
            if (retryCount == maxRetries)
                throw;

            // Wait before retrying
            await Task.Delay(TimeSpan.FromSeconds(5 * retryCount));
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Test endpoint with proper error handling
app.MapPost("/api/orders/test", async (AppDbContext dbContext, IConnection rabbitConnection, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Processing order request");

        // Create a test order
        var order = new Order 
        { 
            UserId = "test-user",
            ProductId = "test-product",  // Changed to string
            UnitPrice = 10.0m,
            Quantity = 2,
            TotalPrice = 20.0m,
            Status = OrderStatus.Pending
        };
        
        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync();
        logger.LogInformation("Order {OrderId} saved to database", order.Id);

        // Create RabbitMQ channel
        using var channel = await rabbitConnection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queue: "orders", durable: false, exclusive: false, autoDelete: false, arguments: null);

        var message = JsonSerializer.Serialize(new { OrderId = order.Id });
        var body = Encoding.UTF8.GetBytes(message);

        await channel.BasicPublishAsync(exchange: "", routingKey: "orders", body: body);
        logger.LogInformation("Order event published to RabbitMQ");

        return Results.Ok(new { OrderId = order.Id, Message = "Order created and event published!" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing order request");
        return Results.Problem(
            title: "Internal Server Error",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError
        );
    }
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public class RabbitMQHealthCheck : IHealthCheck
{
    private readonly IConnection _rabbitConnection;
    private readonly ILogger<RabbitMQHealthCheck> _logger;

    public RabbitMQHealthCheck(IConnection rabbitConnection, ILogger<RabbitMQHealthCheck> logger)
    {
        _rabbitConnection = rabbitConnection;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_rabbitConnection.IsOpen)
            {
                using var channel = await _rabbitConnection.CreateChannelAsync();
                var queueName = $"health_check_{Guid.NewGuid()}";
                await channel.QueueDeclareAsync(queueName, false, true, true, null);
                await channel.QueueDeleteAsync(queueName);

                return HealthCheckResult.Healthy("RabbitMQ connection is healthy");
            }

            return HealthCheckResult.Unhealthy("RabbitMQ connection is closed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ health check failed");
            return HealthCheckResult.Unhealthy("RabbitMQ health check failed", ex);
        }
    }
}