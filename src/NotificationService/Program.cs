using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

var builder = Host.CreateApplicationBuilder(args);

// Configure RabbitMQ
builder.Services.AddSingleton<Task<IConnection>>(async sp =>
{
    
    var logger = sp.GetRequiredService<ILogger<Program>>();
    var retryCount = 0;
    const int maxRetries = 10;

    
    var factory = new ConnectionFactory
    {
        HostName = builder.Configuration["RabbitMQ:Host"],
        UserName = builder.Configuration["RabbitMQ:Username"],
        Password = builder.Configuration["RabbitMQ:Password"]
    };

    while (retryCount < maxRetries)
    {
        try
        {
            return await factory.CreateConnectionAsync();
        }
        catch (Exception ex)
        {
            retryCount++;
            logger.LogError(ex, "RabbitMQ connection failed (Attempt {RetryCount}/{MaxRetries})", 
                retryCount, maxRetries);

            if (retryCount >= maxRetries)
            {
                logger.LogCritical("All RabbitMQ connection attempts failed");
                throw;
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
        }
    }

    throw new Exception("Failed to connect to RabbitMQ after all retries");

});

builder.Services.AddHostedService<NotificationWorker>();

var host = builder.Build();
host.Run();

public class NotificationWorker : BackgroundService
{
    private readonly Task<IConnection> _rabbitConnectionTask;
    private readonly ILogger<NotificationWorker> _logger;
    private IChannel _channel;

    public NotificationWorker(Task<IConnection> rabbitConnectionTask, ILogger<NotificationWorker> logger)
    {
        _rabbitConnectionTask = rabbitConnectionTask;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var connection = await _rabbitConnectionTask;
            _channel = await connection.CreateChannelAsync();
            await _channel.QueueDeclareAsync(
                queue: "orders", 
                durable: false, 
                exclusive: false, 
                autoDelete: false, 
                arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    _logger.LogInformation("Received order: {Message}", message);
                    // TODO: Send email/SMS here
                    
                    await _channel.BasicAckAsync(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message");
                    await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                }
            };

            await _channel.BasicConsumeAsync(
                queue: "orders", 
                autoAck: false, 
                consumer: consumer);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in NotificationWorker");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            if (_channel is not null && _channel.IsOpen)
            {
                await _channel.CloseAsync();
                await _channel.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing RabbitMQ channel");
        }

        await base.StopAsync(cancellationToken);
    }
}