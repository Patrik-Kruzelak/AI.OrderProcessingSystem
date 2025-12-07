using System.Text.Json;
using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.CronJob.Services;
using AI.OrderProcessingSystem.Dal.Data;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Load configuration files
var currentDir = Directory.GetCurrentDirectory();
var secretsPath = Path.Combine(currentDir, "..", "Configuration", "secrets.json");
var instancePath = Path.Combine(currentDir, "..", "Configuration", "instance.json");

if (!File.Exists(secretsPath))
    throw new FileNotFoundException($"Configuration file not found: {secretsPath}");
if (!File.Exists(instancePath))
    throw new FileNotFoundException($"Configuration file not found: {instancePath}");

var secretsConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(secretsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var instanceConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(instancePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Build configuration objects
var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!;

var eventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
    instanceConfig!["EventProcessingSettings"].GetRawText())!;

var rabbitMqSettings = new RabbitMqSettings
{
    Host = instanceConfig["RabbitMqSettings"].GetProperty("Host").GetString()!,
    VirtualHost = instanceConfig["RabbitMqSettings"].GetProperty("VirtualHost").GetString()!,
    Port = instanceConfig["RabbitMqSettings"].GetProperty("Port").GetInt32(),
    Username = secretsConfig!["RabbitMqSettings"].GetProperty("Username").GetString()!,
    Password = secretsConfig["RabbitMqSettings"].GetProperty("Password").GetString()!
};

// Validate configuration
if (string.IsNullOrEmpty(connectionString))
    throw new InvalidOperationException("Database connection string is not configured");
if (string.IsNullOrEmpty(rabbitMqSettings.Host))
    throw new InvalidOperationException("RabbitMQ host is not configured");

// Register configuration
builder.Services.AddSingleton(eventProcessingSettings);
builder.Services.AddSingleton(rabbitMqSettings);

// Add DbContext
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(connectionString));

// Configure MassTransit for publishing events
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitMqSettings.Host, rabbitMqSettings.VirtualHost, h =>
        {
            h.Username(rabbitMqSettings.Username);
            h.Password(rabbitMqSettings.Password);
        });
    });
});

// Register event publisher (stub for publishing)
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

// Register OrderExpiryService as a hosted service
builder.Services.AddHostedService<OrderExpiryService>();

var host = builder.Build();

host.Run();

// MassTransit Event Publisher (same as WebApi)
public class MassTransitEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<MassTransitEventPublisher> _logger;

    public MassTransitEventPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<MassTransitEventPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            await _publishEndpoint.Publish(message, cancellationToken);
            _logger.LogInformation("Published event {EventType}: {@Event}", typeof(T).Name, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}: {@Event}", typeof(T).Name, message);
            throw;
        }
    }
}
