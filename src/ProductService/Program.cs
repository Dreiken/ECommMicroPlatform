using System.Text;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using ProductService.Repositories;
using ProductService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();


// Add MongoDB
var mongoConnection = builder.Configuration.GetConnectionString("ProductDb");
if (string.IsNullOrEmpty(mongoConnection))
{
    throw new InvalidOperationException("MongoDB connection string is missing.");
}

var mongoClient = new MongoClient(mongoConnection);
var database = mongoClient.GetDatabase("ProductDb");
builder.Services.AddSingleton(database);

// Add services
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService.Services.ProductService>();

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? 
                    throw new InvalidOperationException("JWT secret not configured"))
            ),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddHealthChecks()
    .AddMongoDb(
            sp => new MongoClient(mongoConnection),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "db", "mongodb" });



var app = builder.Build();

// Configure middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

//test endpoint
app.MapGet("/api/products/test", async (IMongoDatabase database) =>
{
    var collection = database.GetCollection<BsonDocument>("Products");
    await collection.InsertOneAsync(new BsonDocument { { "name", "Test Product" } });
    return Results.Ok("Product inserted successfully!");
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();
app.UseHttpsRedirection();

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
                description = entry.Value.Description
            })
        };

        await JsonSerializer.SerializeAsync(context.Response.Body, result);
    }
});
app.Run();