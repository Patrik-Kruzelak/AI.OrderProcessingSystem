using System.Text;
using System.Text.Json;
using AI.OrderProcessingSystem.Common.Abstractions;
using AI.OrderProcessingSystem.Common.Configuration;
using AI.OrderProcessingSystem.Dal.Data;
using AI.OrderProcessingSystem.WebApi.Configuration;
using AI.OrderProcessingSystem.WebApi.Services;
using Hellang.Middleware.ProblemDetails;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load configuration files
// Check both the standard location and the content root (for test scenarios)
var currentDir = Directory.GetCurrentDirectory();
var contentRoot = builder.Environment.ContentRootPath;

var secretsPath = Path.Combine(currentDir, "..", "Configuration", "secrets.json");
var instancePath = Path.Combine(currentDir, "..", "Configuration", "instance.json");

// If files don't exist in current directory, try content root (for tests)
if (!File.Exists(secretsPath))
{
    secretsPath = Path.Combine(contentRoot, "Configuration", "secrets.json");
    instancePath = Path.Combine(contentRoot, "Configuration", "instance.json");
}

if (!File.Exists(secretsPath))
    throw new FileNotFoundException($"Configuration file not found: {secretsPath}");
if (!File.Exists(instancePath))
    throw new FileNotFoundException($"Configuration file not found: {instancePath}");

var secretsConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(secretsPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
var instanceConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
    File.ReadAllText(instancePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

// Build merged configuration
var appConfig = new AppConfiguration
{
    ConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                       ?? secretsConfig!["ConnectionStrings"].GetProperty("DefaultConnection").GetString()!,
    JwtSettings = JsonSerializer.Deserialize<JwtSettings>(secretsConfig["JwtSettings"].GetRawText())!,
    AdminUser = JsonSerializer.Deserialize<AdminUserConfig>(secretsConfig["AdminUser"].GetRawText())!,
    AppSettings = JsonSerializer.Deserialize<AppSettings>(instanceConfig!["AppSettings"].GetRawText())!,
    EventProcessingSettings = JsonSerializer.Deserialize<EventProcessingSettings>(
        instanceConfig["EventProcessingSettings"].GetRawText())!,
    RabbitMqSettings = new RabbitMqSettings
    {
        Host = instanceConfig["RabbitMqSettings"].GetProperty("Host").GetString()!,
        VirtualHost = instanceConfig["RabbitMqSettings"].GetProperty("VirtualHost").GetString()!,
        Port = instanceConfig["RabbitMqSettings"].GetProperty("Port").GetInt32(),
        Username = secretsConfig["RabbitMqSettings"].GetProperty("Username").GetString()!,
        Password = secretsConfig["RabbitMqSettings"].GetProperty("Password").GetString()!
    }
};

// Validate configuration
if (string.IsNullOrEmpty(appConfig.ConnectionString))
    throw new InvalidOperationException("Database connection string is not configured");
if (string.IsNullOrEmpty(appConfig.JwtSettings.SecretKey) || appConfig.JwtSettings.SecretKey.Length < 32)
    throw new InvalidOperationException("JWT SecretKey must be at least 32 characters");
if (string.IsNullOrEmpty(appConfig.RabbitMqSettings.Host))
    throw new InvalidOperationException("RabbitMQ host is not configured");
if (appConfig.EventProcessingSettings.OrderCompletionSuccessRate < 0 ||
    appConfig.EventProcessingSettings.OrderCompletionSuccessRate > 1)
    throw new InvalidOperationException("OrderCompletionSuccessRate must be between 0 and 1");

// Register configuration
builder.Services.AddSingleton(appConfig.JwtSettings);
builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(appConfig.EventProcessingSettings);
builder.Services.AddSingleton(appConfig.RabbitMqSettings);

// Add DbContext
builder.Services.AddDbContext<OrderProcessingDbContext>(options =>
    options.UseNpgsql(appConfig.ConnectionString));

// Add services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IEventPublisher, MassTransitEventPublisher>();

// Add MassTransit with RabbitMQ
builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(appConfig.RabbitMqSettings.Host, appConfig.RabbitMqSettings.VirtualHost, h =>
        {
            h.Username(appConfig.RabbitMqSettings.Username);
            h.Password(appConfig.RabbitMqSettings.Password);
        });

        // Configure message topology
        cfg.ConfigureEndpoints(context);
    });
});

// Add controllers
builder.Services.AddControllers();

// Add ProblemDetails middleware
builder.Services.AddProblemDetails(options =>
{
    options.IncludeExceptionDetails = (ctx, ex) => builder.Environment.IsDevelopment();
    options.MapToStatusCode<NotImplementedException>(StatusCodes.Status501NotImplemented);
    options.MapToStatusCode<UnauthorizedAccessException>(StatusCodes.Status401Unauthorized);
});

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = appConfig.JwtSettings.Issuer,
            ValidAudience = appConfig.JwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(appConfig.JwtSettings.SecretKey))
        };
    });

builder.Services.AddAuthorization();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = appConfig.AppSettings.ApiTitle,
        Version = appConfig.AppSettings.ApiVersion,
        Description = appConfig.AppSettings.ApiDescription
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Use ProblemDetails middleware
app.UseProblemDetails();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{appConfig.AppSettings.ApiTitle} {appConfig.AppSettings.ApiVersion}");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Run migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<OrderProcessingDbContext>();
    dbContext.Database.Migrate();
}

app.Run();

// Make Program class public for testing
public partial class Program { }
