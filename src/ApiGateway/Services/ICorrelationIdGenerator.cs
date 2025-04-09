namespace ApiGateway.Services;

public interface ICorrelationIdGenerator
{
    string Get();
    void Set(string correlationId);
}