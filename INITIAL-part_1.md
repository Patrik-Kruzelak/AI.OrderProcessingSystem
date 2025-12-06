## FEATURE:

In this project, I need to create a PostgreSQL database that will be set up via Docker, which means we must also create a Dockerfile that will be added to the Git repository.

Using this Dockerfile, I would also like to run the Web API project.

Next, we need to create the following tables:

- User has the following fields: id, name (max length 100), email (max length 100 and unique), password string
- Product has the following fields: id, name (string, max length 100), description (string), price (number ≥ 0), stock (number ≥ 0), created_at (timestamp)
- Order has the following fields: id, user_id, total (number ≥ 0), status enum (pending, processing, completed, expired), items schema id (primary key), product_id, quantity (number > 0), price (number > 0), created_at (timestamp), updated_at (timestamp)

These database models should be created in the project AI.OrderProcessingSystem.Dal, where the database context will also be added. I would like to use Entity Framework in this project. This project should also contain all migration files.

For application usage, I will also need to create an Admin user, which should be created immediately via seed, and at the end provide the password.

Once we have the database and the Admin user ready, we will need endpoints in AI.OrderProcessingSystem.WebApi. The first thing we should implement is the Login REST API – Check user credentials (email, password) and if correct, return JWT token. Use best practices when generating the token.

Next, for the existing database/models, we need to create endpoints in the Web API project:

- User: Create CRUD REST API for this module. Validate input DTOs; if invalid, return 400.
- Product: Create CRUD REST API for this module. Validate input DTOs; if invalid, return 400.
- Order: Create CRUD REST API for this module. Validate input DTOs. The rules are defined in the schema.

Additional requirements:
- Endpoints must be secured with a JWT Bearer token (result of the Login REST API).
- Correctly handle error return states (400 Bad Request, 401 Unauthorized, 404 Not Found, 500 Internal Server Error, etc.).
- Include OpenAPI/Swagger documentation.
- Integration tests (minimum 5 test cases).
- Use PostgreSQL DB. Run PostgreSQL in Docker and initialize it using the docker compose file. Include the docker compose file in the Git repository.
- Include a DB upgrade mechanism in the final solution. It must contain some form of DB upgrade scripts or DB upgrade code.
- Also include initial seed data in the DB; it can be part of the upgrade mechanism as well.
- In README.md, document how to run the DB upgrade tool and how to start the service.

If necessary, adjust the type of existing projects that are currently created as console applications. The console applications were generated only as an initial skeleton.

## EXAMPLES:

### Existing Project Structure Reference

**AI.OrderProcessingSystem.WebApi** - Main ASP.NET Core MVC application
- Current entry point: `AI.OrderProcessingSystem.WebApi/Program.cs:1`
- Existing controller pattern: `AI.OrderProcessingSystem.WebApi/Controllers/HomeController.cs:7` (HomeController)
- Controller uses dependency injection for ILogger at `AI.OrderProcessingSystem.WebApi/Controllers/HomeController.cs:11`
- Current middleware pipeline configured at `AI.OrderProcessingSystem.WebApi/Program.cs:8-25`
- Note: Currently only has MVC setup, needs to be extended with API controllers and JWT authentication

**AI.OrderProcessingSystem.Dal** - Data Access Layer project
- Currently: Console application (`AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj:4` shows OutputType: Exe)
- Needs conversion: Change from console app to class library (remove OutputType or set to Library)
- This is where DbContext, entities, and EF migrations should be added
- Current state: Only has placeholder "Hello, World!" in `AI.OrderProcessingSystem.Dal/Program.cs`

**AI.OrderProcessingSystem.Common** - Shared utilities
- Currently: Console application (needs conversion to class library)
- Should contain: DTOs, validation attributes, shared constants
- Must not reference any other project (see `CLAUDE.md:86`)

### Configuration Pattern to Follow

Per `CLAUDE.md:113-127`, the system must:
1. Load `\Configuration\secrets.json` (contains DB credentials, JWT secret key, etc.)
2. Load `\Configuration\instance.json` (contains DB URLs, non-sensitive config)
3. Merge into single runtime configuration object
4. Validate both at startup in `AI.OrderProcessingSystem.WebApi/Program.cs`

Note: Configuration folder does not exist yet and needs to be created at repository root.

### Docker Configuration Location

Per `CLAUDE.md:129-136`, Docker files must be at repository root:
- `\Dockerfile` - For building the WebApi project
- `\docker-compose.yml` - For PostgreSQL and potentially the WebApi service
- Must reference `instance.json` and `secrets.json` for environment configuration

### Controller Pattern to Follow

Look at `AI.OrderProcessingSystem.WebApi/Controllers/HomeController.cs:7-32` for the existing pattern:
- Controllers inherit from `Controller` or `ControllerBase` (use ControllerBase for APIs)
- Use dependency injection in constructor (line 11)
- Action methods return `IActionResult`
- Use attributes like `[ResponseCache]` for caching (line 26)

For REST APIs, create new controllers following this pattern but:
- Inherit from `ControllerBase` instead of `Controller`
- Use `[ApiController]` attribute
- Use `[Route("api/[controller]")]` attribute
- Use `[Authorize]` attribute for JWT-protected endpoints

### Dependency Injection Pattern

Current DI setup at `AI.OrderProcessingSystem.WebApi/Program.cs:4`:
```csharp
builder.Services.AddControllersWithViews();
```

Will need to extend with:
- DbContext registration
- JWT authentication services
- Repository/service registrations
- Swagger/OpenAPI services

## DOCUMENTATION:

### .NET & Entity Framework Core
- [Entity Framework Core with PostgreSQL](https://learn.microsoft.com/en-us/ef/core/providers/npgsql/)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Data Seeding in EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)

### ASP.NET Core Authentication
- [JWT Bearer Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [ASP.NET Core Web API Best Practices](https://learn.microsoft.com/en-us/aspnet/core/web-api/)

### OpenAPI/Swagger
- [Swashbuckle for ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle)

### Docker
- [Docker Compose for PostgreSQL](https://hub.docker.com/_/postgres)
- [Dockerizing ASP.NET Core Apps](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

### Testing
- [Integration Testing in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)

### Internal Documentation
- `CLAUDE.md` - Complete project structure, architecture rules, and build commands
- `AI.OrderProcessingSystem.sln` - Solution file at repository root

## OTHER CONSIDERATIONS:

### Project Conversion Requirements

**IMPORTANT**: Several projects need conversion from console apps to class libraries:
- `AI.OrderProcessingSystem.Dal/AI.OrderProcessingSystem.Dal.csproj:4` - Remove `<OutputType>Exe</OutputType>` or change to `Library`
- `AI.OrderProcessingSystem.Common/AI.OrderProcessingSystem.Common.csproj` - Same conversion needed
- After conversion, remove the `Program.cs` files from these projects as they won't be needed

### Project Reference Rules (from CLAUDE.md:72-104)

**AI.OrderProcessingSystem.WebApi** can reference:
- AI.OrderProcessingSystem.Common ✓
- AI.OrderProcessingSystem.Dal ✓
- Cannot reference: AI.OrderProcessingSystem.Worker, AI.OrderProcessingSystem.CronJob ✗

**AI.OrderProcessingSystem.Dal** can reference:
- AI.OrderProcessingSystem.Common ✓
- Cannot reference: AI.OrderProcessingSystem.Common (per CLAUDE.md:92) - wait, this is contradictory. Re-read: Dal can be referenced by all except Common, so Dal CAN reference Common.

**AI.OrderProcessingSystem.Common**:
- Must not reference any other project (CLAUDE.md:86)

### JWT Token Best Practices
- Store JWT secret in `\Configuration\secrets.json`
- Use strong secret key (at least 256 bits for HS256)
- Set appropriate token expiration (e.g., 1 hour for access tokens)
- Include claims: user id, email, roles
- Use `[Authorize]` attribute on protected endpoints
- Return 401 for invalid/expired tokens

### Entity Framework Gotchas
- DbContext should be registered as scoped service
- Connection string must come from merged configuration (secrets.json + instance.json)
- Run migrations on application startup OR provide a separate migration tool
- Use `OnModelCreating` for entity configuration and seed data
- Password field for User entity should store hashed passwords (use BCrypt or ASP.NET Core Identity hashing)

### Validation Patterns
- Use Data Annotations on DTOs (e.g., `[Required]`, `[MaxLength(100)]`, `[EmailAddress]`)
- Use `[ApiController]` attribute for automatic model validation (returns 400 automatically)
- For complex validation, implement custom validation attributes or FluentValidation

### Error Handling
- Current setup has basic exception handler at `AI.OrderProcessingSystem.WebApi/Program.cs:11`
- Extend with custom exception middleware for consistent error responses
- Use ProblemDetails for error responses (RFC 7807 standard)

### Swagger/OpenAPI Setup
- Configure JWT bearer authentication in Swagger UI
- Add XML comments for better API documentation
- Group endpoints by tags (User, Product, Order, Auth)

### PostgreSQL Connection String Format
Store in `\Configuration\secrets.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=orderprocessing;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here",
    "Issuer": "AI.OrderProcessingSystem",
    "Audience": "AI.OrderProcessingSystem.WebApi"
  }
}
```

### README.md Requirements
The README.md file needs to be created with:
1. How to run DB migrations (e.g., `dotnet ef database update` from Dal project)
2. How to start services using docker-compose
3. How to run the WebApi project
4. Default admin credentials (after seed)
5. Swagger UI URL (likely `https://localhost:7037/swagger`)

### Testing Considerations
- Create a separate test project (e.g., `AI.OrderProcessingSystem.WebApi.Tests`)
- Use WebApplicationFactory for integration tests
- Use in-memory database or test containers for isolated tests
- Minimum 5 test cases should cover: CRUD operations, authentication, validation failures
