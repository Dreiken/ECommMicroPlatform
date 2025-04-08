using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProductService.Repositories;
using ProductService.Services;

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


builder.Services.AddHealthChecks()
    .AddMongoDb(
            sp => new MongoClient(mongoConnection),
        name: "mongodb",
        timeout: TimeSpan.FromSeconds(3),
        tags: new[] { "db", "mongodb" });



var app = builder.Build();

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