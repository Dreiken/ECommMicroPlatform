using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace ApiGateway.Handlers;

public class CircuitBreakerLoggingHandler : DelegatingHandler
{
    private readonly ILogger<CircuitBreakerLoggingHandler> _logger;

    public CircuitBreakerLoggingHandler(ILogger<CircuitBreakerLoggingHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            _logger.LogInformation(
                "Request {Method} {Path} completed with status {StatusCode}",
                request.Method,
                request.RequestUri?.PathAndQuery,
                response.StatusCode);
            return response;
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Circuit breaker prevented request to {Method} {Path}",
                request.Method,
                request.RequestUri?.PathAndQuery);
            throw;
        }
    }
}