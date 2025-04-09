using ApiGateway.Services;

namespace ApiGateway.Middleware;

public class CorrelationIdMiddleware : IMiddleware
{
    private readonly ICorrelationIdGenerator _correlationIdGenerator;
    private readonly ILogger<CorrelationIdMiddleware> _logger;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(
        ICorrelationIdGenerator correlationIdGenerator,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _correlationIdGenerator = correlationIdGenerator;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = GetOrGenerateCorrelationId(context);
        _correlationIdGenerator.Set(correlationId);

        AddCorrelationIdHeader(context, correlationId);
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            _logger.LogInformation(
                "Processing request {Method} {Path} with correlation ID {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await next(context);

            _logger.LogInformation(
                "Completed request {Method} {Path} with correlation ID {CorrelationId}. Status code: {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                context.Response.StatusCode);
        }
    }

    private static string GetOrGenerateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId))
        {
            return existingCorrelationId.ToString();
        }
        return Guid.NewGuid().ToString();
    }

    private static void AddCorrelationIdHeader(HttpContext context, string correlationId)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });
    }
}