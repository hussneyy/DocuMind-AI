using System.Threading.RateLimiting;
using DocuMind.Api.Middleware;
using DocuMind.Application;
using DocuMind.Infrastructure;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "DocuMind AI API",
        Version = "v1",
        Description = "Intelligent document search and AI knowledge assistant API."
    });
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "documind.auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;

    // This is an API, not a Razor app — return status codes instead of redirecting to a login page.
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

// Configurable so the integration test host (many rapid register/login calls across a shared
// test collection) can raise the limit without weakening the real default.
var authRateLimitPermits = builder.Configuration.GetValue("RateLimiting:Auth:PermitLimit", 10);
var authRateLimitWindowSeconds = builder.Configuration.GetValue("RateLimiting:Auth:WindowSeconds", 60);

// Upload and "ai" (anything that triggers a real LLM call — chat ask, summary generation) get
// their own limiters: both are meaningfully more expensive per-request than a typical read, so
// they're throttled separately from the general request pipeline rather than left unbounded.
var uploadRateLimitPermits = builder.Configuration.GetValue("RateLimiting:Upload:PermitLimit", 20);
var uploadRateLimitWindowSeconds = builder.Configuration.GetValue("RateLimiting:Upload:WindowSeconds", 60);
var aiRateLimitPermits = builder.Configuration.GetValue("RateLimiting:Ai:PermitLimit", 30);
var aiRateLimitWindowSeconds = builder.Configuration.GetValue("RateLimiting:Ai:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = authRateLimitPermits;
        limiterOptions.Window = TimeSpan.FromSeconds(authRateLimitWindowSeconds);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("upload", limiterOptions =>
    {
        limiterOptions.PermitLimit = uploadRateLimitPermits;
        limiterOptions.Window = TimeSpan.FromSeconds(uploadRateLimitWindowSeconds);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
    options.AddFixedWindowLimiter("ai", limiterOptions =>
    {
        limiterOptions.PermitLimit = aiRateLimitPermits;
        limiterOptions.Window = TimeSpan.FromSeconds(aiRateLimitWindowSeconds);
        limiterOptions.QueueLimit = 0;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

builder.Services.AddHealthChecks()
    .AddNpgSql(connectionString, name: "postgres", tags: new[] { "ready" });

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Opt-in, off by default: local `dotnet run` development uses `dotnet-ef database update`
// explicitly (see README), and the integration test factory migrates itself in InitializeAsync.
// This exists so the Docker Compose stack — which has no separate migration step — is otherwise
// self-contained; it only runs when Database:MigrateOnStartup is explicitly set, which only
// docker-compose.yml does.
if (app.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<DocuMind.Infrastructure.Persistence.AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocuMind AI API v1");
    });
}
else
{
    // Tells browsers to remember this host should only ever be reached over HTTPS — meaningless
    // in local Development (no real TLS termination to enforce), so only enabled otherwise.
    app.UseHsts();
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseRateLimiter();

app.UseMiddleware<CsrfProtectionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Liveness: process is up. Readiness: dependencies (database) are reachable.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

/// <summary>Exposed so WebApplicationFactory&lt;Program&gt; can host this app in integration tests.</summary>
public partial class Program;
