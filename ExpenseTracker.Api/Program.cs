using System.Text;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Threading.RateLimiting;

// Program.cs is the composition root: it wires configuration, DI, middleware, auth, and startup database work.
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

var useInMemoryDatabase = builder.Environment.IsEnvironment("Testing") || builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"];

ValidateSecurityConfiguration(builder.Configuration, useInMemoryDatabase);

if (useInMemoryDatabase)
{
    // Tests use an isolated in-memory database instead of PostgreSQL.
    var databaseName = builder.Configuration["InMemoryDatabaseName"] ?? $"expense-tracker-{Guid.NewGuid()}";
    builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
}

builder.Services.AddControllers(); // knows by classes that inherit from ControllerBase and ones
                                // with [ApiController]
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IFinancialMessageService, FinancialMessageService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginAttemptTracker, MemoryLoginAttemptTracker>();
builder.Services
    .AddHttpClient<IAiClassificationService, GeminiAiClassificationService>()
    .RemoveAllLoggers();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
var jwtSigningKey = jwtOptions.Key;
if (useInMemoryDatabase && string.IsNullOrWhiteSpace(jwtSigningKey))
{
    jwtSigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    builder.Services.PostConfigure<JwtOptions>(options => options.Key = jwtSigningKey);
}

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIdentifier(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 120,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIdentifier(context),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.AddPolicy("ai", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIdentifier(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = (rateLimitContext, cancellationToken) =>
    {
        var logger = rateLimitContext.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("RateLimiting");
        logger.LogWarning(
            "Rate limit exceeded for {Path} from {ClientIdentifier}.",
            rateLimitContext.HttpContext.Request.Path,
            GetClientIdentifier(rateLimitContext.HttpContext));

        rateLimitContext.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };
});
// AddCors is needed to allow the frontend (which runs on a different origin during development) to make requests to the API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "https://localhost:5173",
                "http://127.0.0.1:5173",
                "https://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Build the app, then apply any pending database migrations and seed baseline data before starting to accept requests.
var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// This build the middleware pipeline that every request goes through. Order matters.
// "what happens to every request before it reaches a controller?"
app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers(); // "take all controller actions and make them reachable by HTTP"
/* What it actually does (app.MapControllers();): 
Scans the project for classes marked with [ApiController]
Reads their [Route] and HTTP attributes ([HttpGet], [HttpPost], etc.)
Creates endpoints from them
Hooks those endpoints into the request pipeline */

using (var scope = app.Services.CreateScope()) // Create a scope to get scoped services like AppDbContext
{
    // Apply schema changes on startup, then make sure baseline data exists.
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (dbContext.Database.IsRelational()) 
    {
        dbContext.Database.Migrate();
    }
    else
    {
        dbContext.Database.EnsureCreated();
    }

    await AppDbSeeder.SeedAsync(dbContext); 
}

app.Run(); 

void ValidateSecurityConfiguration(ConfigurationManager configuration, bool useInMemoryDatabase)
{
    if (!useInMemoryDatabase && string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")) && string.IsNullOrWhiteSpace(configuration["DATABASE_URL"]))
    {
        throw new InvalidOperationException(
            "Database connection string is required. Configure ConnectionStrings__DefaultConnection or DATABASE_URL through environment variables, user-secrets, or a secret manager.");
    }

    var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>();
    if (jwtOptions is null)
    {
        throw new InvalidOperationException("JWT configuration is required.");
    }

    if (!useInMemoryDatabase && (string.IsNullOrWhiteSpace(jwtOptions.Key) || jwtOptions.Key.Length < 32))
    {
        throw new InvalidOperationException(
            "JWT signing key is missing or too weak. Configure Jwt__Key with at least 32 characters through environment variables, user-secrets, or a secret manager.");
    }
}

string GetClientIdentifier(HttpContext context)
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public partial class Program;
