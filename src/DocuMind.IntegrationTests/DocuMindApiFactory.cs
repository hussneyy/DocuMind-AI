using DocuMind.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.IntegrationTests;

/// <summary>
/// Hosts the real API in-process against a dedicated "documind_test" database (created once on
/// the same local Postgres container used for development) rather than mocks, so these tests
/// exercise real EF Core migrations, real Identity cookie auth, and real ownership-filtered
/// queries end to end.
/// </summary>
public class DocuMindApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string StorageDirectory { get; } =
        Path.Combine(Path.GetTempPath(), "documind-tests-storage-" + Guid.NewGuid());

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=localhost;Port=5432;Database=documind_test;Username=documind;Password=documind_dev_local_only",
                ["Storage:Directory"] = StorageDirectory,
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync()
    {
        if (Directory.Exists(StorageDirectory))
        {
            Directory.Delete(StorageDirectory, recursive: true);
        }

        return base.DisposeAsync().AsTask();
    }
}
