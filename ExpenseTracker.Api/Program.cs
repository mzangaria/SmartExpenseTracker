using System.Text;
using ExpenseTracker.Api.Configuration;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Entities;
using ExpenseTracker.Api.Services;
using ExpenseTracker.Api.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;

// Program.cs is the composition root: it wires configuration, DI, middleware, auth, and startup database work.
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["DATABASE_URL"]
    ?? "Host=localhost;Port=5432;Database=expense_tracker;Username=postgres;Password=postgres";

var useInMemoryDatabase = builder.Environment.IsEnvironment("Testing") || builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
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
builder.Services.AddHttpClient<IAiClassificationService, GeminiAiClassificationService>();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

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

public partial class Program;
