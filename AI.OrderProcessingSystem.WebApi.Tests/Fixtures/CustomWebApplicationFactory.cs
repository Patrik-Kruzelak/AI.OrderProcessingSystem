using System.Text.Json;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.WebApi.Configuration;
using AI.OrderProcessingSystem.WebApi.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;

namespace AI.OrderProcessingSystem.WebApi.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("orderprocessing_test")
        .WithUsername("postgres")
        .WithPassword("test")
        .Build();

    private string? _testConfigPath;
    private static readonly object _configFileLock = new object();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set the content root to the solution root where Configuration folder will be
        var testBinaryPath = AppDomain.CurrentDomain.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(testBinaryPath, "..", "..", "..", ".."));

        // Create test configuration files BEFORE setting content root
        // This ensures they exist when Program.cs tries to load them
        CreateTestConfigurationFilesSync(solutionRoot);

        builder.UseContentRoot(solutionRoot);

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderProcessingDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // Remove existing AppConfiguration
            var appConfigDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(AppConfiguration));
            if (appConfigDescriptor != null)
                services.Remove(appConfigDescriptor);

            // Add test database
            services.AddDbContext<OrderProcessingDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Add test configuration
            var testConfig = new AppConfiguration
            {
                ConnectionString = _dbContainer.GetConnectionString(),
                JwtSettings = new JwtSettings
                {
                    SecretKey = "test-secret-key-with-minimum-32-characters-for-hs256-algorithm",
                    Issuer = "TestIssuer",
                    Audience = "TestAudience",
                    ExpirationMinutes = 60
                },
                AdminUser = new AdminUserConfig
                {
                    Email = "admin@orderprocessing.local",
                    Password = "Admin@12345"
                },
                AppSettings = new AppSettings
                {
                    ApiTitle = "Test API",
                    ApiVersion = "v1",
                    ApiDescription = "Test API Description"
                },
                EventProcessingSettings = new EventProcessingSettings
                {
                    PaymentProcessingDelaySeconds = 1,
                    OrderCompletionSuccessRate = 0.8,
                    OrderExpiryThresholdMinutes = 5,
                    ExpiryCheckIntervalSeconds = 30
                },
                RabbitMqSettings = new RabbitMqSettings
                {
                    Host = "localhost",
                    VirtualHost = "/",
                    Port = 5672,
                    Username = "test",
                    Password = "test"
                }
            };

            services.AddSingleton(testConfig);
            services.AddSingleton(testConfig.JwtSettings);
            services.AddSingleton(testConfig.EventProcessingSettings);
            services.AddSingleton(testConfig.RabbitMqSettings);

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
            db.Database.Migrate();

            // Ensure admin user exists with correct password
            EnsureAdminUserSeeded(db, testConfig);
        });
    }

    private static void EnsureAdminUserSeeded(OrderProcessingDbContext context, AppConfiguration config)
    {
        var adminUser = context.Users.FirstOrDefault(u => u.Email == config.AdminUser.Email);
        if (adminUser != null)
        {
            // Update existing admin user password to ensure it matches test expectations
            adminUser.Password = BCrypt.Net.BCrypt.HashPassword(config.AdminUser.Password);
            context.SaveChanges();
        }
    }

    private void CreateTestConfigurationFilesSync(string solutionRoot)
    {
        lock (_configFileLock)
        {
            _testConfigPath = Path.Combine(solutionRoot, "Configuration");

            // Ensure Configuration directory exists
            if (!Directory.Exists(_testConfigPath))
            {
                Directory.CreateDirectory(_testConfigPath);
            }

            var secretsPath = Path.Combine(_testConfigPath, "secrets.json");
            var instancePath = Path.Combine(_testConfigPath, "instance.json");

            // Only write files if they don't already exist (to avoid file locking issues)
            if (!File.Exists(secretsPath) || !File.Exists(instancePath))
            {
                var secretsConfig = new
                {
                    ConnectionStrings = new { DefaultConnection = _dbContainer.GetConnectionString() },
                    JwtSettings = new
                    {
                        SecretKey = "test-secret-key-with-minimum-32-characters-for-hs256-algorithm",
                        Issuer = "TestIssuer",
                        Audience = "TestAudience",
                        ExpirationMinutes = 60
                    },
                    AdminUser = new
                    {
                        Email = "admin@orderprocessing.local",
                        Password = "Admin@12345"
                    },
                    RabbitMqSettings = new
                    {
                        Username = "test",
                        Password = "test"
                    }
                };

                var instanceConfig = new
                {
                    AppSettings = new
                    {
                        ApiTitle = "Test API",
                        ApiVersion = "v1",
                        ApiDescription = "Test API Description"
                    },
                    EventProcessingSettings = new
                    {
                        PaymentProcessingDelaySeconds = 1,
                        OrderCompletionSuccessRate = 0.8,
                        OrderExpiryThresholdMinutes = 5,
                        ExpiryCheckIntervalSeconds = 30
                    },
                    RabbitMqSettings = new
                    {
                        Host = "localhost",
                        VirtualHost = "/",
                        Port = 5672
                    }
                };

                if (!File.Exists(secretsPath))
                {
                    File.WriteAllText(secretsPath, JsonSerializer.Serialize(secretsConfig, new JsonSerializerOptions { WriteIndented = true }));
                }

                if (!File.Exists(instancePath))
                {
                    File.WriteAllText(instancePath, JsonSerializer.Serialize(instanceConfig, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();

        // Clean up test config files
        if (_testConfigPath != null && Directory.Exists(_testConfigPath))
        {
            try
            {
                Directory.Delete(_testConfigPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
