using System.Text.Json;
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Create test configuration files in the solution root
        CreateTestConfigurationFiles();

        // Set the content root to the solution root where Configuration folder is
        var testBinaryPath = AppDomain.CurrentDomain.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(testBinaryPath, "..", "..", "..", ".."));
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
                }
            };

            services.AddSingleton(testConfig);
            services.AddSingleton(testConfig.JwtSettings);

            // Ensure database is created and migrated
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
            db.Database.Migrate();
        });
    }

    private void CreateTestConfigurationFiles()
    {
        // Get the test binary directory and go up to find Configuration folder
        var testBinaryPath = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = Path.GetFullPath(Path.Combine(testBinaryPath, "..", "..", "..", ".."));
        _testConfigPath = Path.Combine(projectRoot, "Configuration");

        // Ensure Configuration directory exists
        if (!Directory.Exists(_testConfigPath))
        {
            Directory.CreateDirectory(_testConfigPath);
        }

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
            }
        };

        var instanceConfig = new
        {
            AppSettings = new
            {
                ApiTitle = "Test API",
                ApiVersion = "v1",
                ApiDescription = "Test API Description"
            }
        };

        var secretsPath = Path.Combine(_testConfigPath, "secrets.json");
        var instancePath = Path.Combine(_testConfigPath, "instance.json");

        File.WriteAllText(secretsPath, JsonSerializer.Serialize(secretsConfig, new JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(instancePath, JsonSerializer.Serialize(instanceConfig, new JsonSerializerOptions { WriteIndented = true }));
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
