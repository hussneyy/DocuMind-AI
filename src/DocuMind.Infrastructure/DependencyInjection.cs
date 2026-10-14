using DocuMind.Application.Auth;
using DocuMind.Application.Documents;
using DocuMind.Infrastructure.Documents;
using DocuMind.Infrastructure.Identity;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<IPdfInspector, PdfPigInspector>();

        return services;
    }
}
