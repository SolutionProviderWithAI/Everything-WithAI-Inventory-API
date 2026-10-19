using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using InventoryOrderSystem.Api.Middlewares;
using InventoryOrderSystem.Core.Interfaces;
using InventoryOrderSystem.Infrastructure.Data;
using InventoryOrderSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Controller and JSON serialization setup
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// 2. Database Context configuration
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
var postgresCs = builder.Configuration.GetConnectionString("PostgreSQL");
var sqliteCs = builder.Configuration.GetConnectionString("Sqlite") ?? "Data Source=inventory_system.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(postgresCs))
    {
        options.UseNpgsql(postgresCs, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlite(sqliteCs);
    }
});

// 3. Register Domain Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// 4. JWT Authentication configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecureInventoryManagementJwtKey2026!@#$%^&*()_+ExtraLongSecretForSecurityCompliance";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "InventoryOrderSystem";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "InventoryOrderClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireStaff", policy => policy.RequireRole("Admin", "Staff"));
    options.AddPolicy("RequireCustomer", policy => policy.RequireRole("Customer", "Admin"));
});

// 5. CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 6. Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Chatbot-Based Order & Inventory Management API",
        Version = "v1",
        Description = "Production-ready backend API for Inventory Control, Order Processing, and Chatbot Orchestration.",
        Contact = new OpenApiContact
        {
            Name = "Apex Inventory Engineering",
            Email = "support@inventorysystem.com"
        }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 7. Auto Database Migration and Seeding with Fallback
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        logger.LogInformation("Verifying database schema and initial data seeding...");
        await context.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(context);
        logger.LogInformation("Database verified and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not initialize primary database provider. Attempting local SQLite fallback...");
        try
        {
            var sqliteOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(sqliteCs)
                .Options;

            using var fallbackContext = new AppDbContext(sqliteOptions);
            await fallbackContext.Database.EnsureCreatedAsync();
            await DbSeeder.SeedAsync(fallbackContext);
            logger.LogInformation("Fallback SQLite database initialized successfully.");
        }
        catch (Exception fallbackEx)
        {
            logger.LogError(fallbackEx, "Fatal: failed to initialize database.");
        }
    }
}

// 8. Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory & Order API v1");
    c.RoutePrefix = "swagger";
});

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.Run();

// Required for WebApplicationFactory in integration tests
public partial class Program { }
