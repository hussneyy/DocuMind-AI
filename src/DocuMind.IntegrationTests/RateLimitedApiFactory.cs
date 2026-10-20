using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.IntegrationTests;

/// <summary>
/// A separate, single-use host (not part of <see cref="DocuMindApiCollection"/>) with the "upload"
/// and "ai" rate limits deliberately set very low, so tests here can actually exceed them within
/// a handful of requests. The shared collection intentionally raises every limit instead (to avoid
/// 429s during normal test runs), so exercising the limiter itself needs its own isolated instance.
/// </summary>
public class RateLimitedApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string StorageDirectory { get; } =
        Path.Combine(Path.GetTempPath(), "documind-tests-ratelimit-storage-" + Guid.NewGuid());

    public RateLimitedApiFactory()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Postgres",
            "Host=localhost;Port=5432;Database=documind_test;Username=documind;Password=documind_dev_local_only");
        Environment.SetEnvironmentVariable("Storage__Directory", StorageDirectory);

        Environment.SetEnvironmentVariable("RateLimiting__Auth__PermitLimit", "1000");
        Environment.SetEnvironmentVariable("RateLimiting__Auth__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("RateLimiting__Upload__PermitLimit", "2");
        Environment.SetEnvironmentVariable("RateLimiting__Upload__WindowSeconds", "60");
        Environment.SetEnvironmentVariable("RateLimiting__Ai__PermitLimit", "2");
        Environment.SetEnvironmentVariable("RateLimiting__Ai__WindowSeconds", "60");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();

        if (Directory.Exists(StorageDirectory))
        {
            Directory.Delete(StorageDirectory, recursive: true);
        }
    }
}
