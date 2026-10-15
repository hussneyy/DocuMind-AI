using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.IntegrationTests;

/// <summary>
/// Hosts the real API in-process against a dedicated "documind_test" database (created once on
/// the same local Postgres container used for development) rather than mocks, so these tests
/// exercise real EF Core migrations, real Identity cookie auth, and real ownership-filtered
/// queries end to end.
///
/// Configuration is overridden via process environment variables set in the constructor — NOT
/// via <c>IWebHostBuilder.ConfigureAppConfiguration</c>, which was tried first and silently did
/// not take effect for this minimal-hosting Program.cs (confirmed by direct diagnostics: the app
/// kept reading the appsettings.json placeholder regardless). Environment variables are read
/// unconditionally and early by <c>WebApplication.CreateBuilder</c>, so this is the reliable path.
/// </summary>
public class DocuMindApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string StorageDirectory { get; } =
        Path.Combine(Path.GetTempPath(), "documind-tests-storage-" + Guid.NewGuid());

    public DocuMindApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=documind_test;Username=documind;Password=documind_dev_local_only");
        Environment.SetEnvironmentVariable("Storage__Directory", StorageDirectory);

        // The whole suite shares one host (see DocuMindApiCollection) and makes many rapid
        // register/login calls — raise the limit well above the production default rather than
        // disabling it, so the limiter itself still gets exercised implicitly.
        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowSeconds", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Also disables ASP.NET Core's automatic `dotnet user-secrets` loading (gated on
        // IsDevelopment()), which otherwise silently overrides the environment variables above
        // with the real local dev connection string.
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        // Dispose the host (and any open file handles it holds) before touching the directory it
        // was writing into — deleting first risks an IOException from a handle that hasn't closed yet.
        await base.DisposeAsync();

        if (Directory.Exists(StorageDirectory))
        {
            Directory.Delete(StorageDirectory, recursive: true);
        }
    }
}
