using AI.OrderProcessingSystem.Common.Configuration;

namespace AI.OrderProcessingSystem.WebApi.Configuration;

public class AppConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public JwtSettings JwtSettings { get; set; } = new();
    public AdminUserConfig AdminUser { get; set; } = new();
    public AppSettings AppSettings { get; set; } = new();
    public EventProcessingSettings EventProcessingSettings { get; set; } = new();
    public RabbitMqSettings RabbitMqSettings { get; set; } = new();
}

public class AdminUserConfig
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AppSettings
{
    public string ApiTitle { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = string.Empty;
    public string ApiDescription { get; set; } = string.Empty;
}
