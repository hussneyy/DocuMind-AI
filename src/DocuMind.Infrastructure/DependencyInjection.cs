using Amazon.S3;
using DocuMind.Application.Auditing;
using DocuMind.Application.Auth;
using DocuMind.Application.Chat;
using DocuMind.Application.Documents;
using DocuMind.Application.Embeddings;
using DocuMind.Application.Llm;
using DocuMind.Application.Summaries;
using DocuMind.Application.Tags;
using DocuMind.Infrastructure.Auditing;
using DocuMind.Infrastructure.BackgroundJobs;
using DocuMind.Infrastructure.Documents;
using DocuMind.Infrastructure.Embeddings;
using DocuMind.Infrastructure.Identity;
using DocuMind.Infrastructure.Llm;
using DocuMind.Infrastructure.Persistence;
using DocuMind.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddSingleton<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ISummaryRepository, SummaryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var region = sp.GetRequiredService<IOptions<StorageOptions>>().Value.Region;
            return new AmazonS3Client(Amazon.RegionEndpoint.GetBySystemName(region));
        });
        services.AddSingleton<IFileStorage>(sp =>
        {
            var storageOptions = sp.GetRequiredService<IOptions<StorageOptions>>().Value;
            return storageOptions.Provider.Equals("S3", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<S3FileStorage>(sp)
                : ActivatorUtilities.CreateInstance<LocalFileStorage>(sp);
        });
        services.AddScoped<IPdfInspector, PdfPigInspector>();
        services.AddScoped<IPdfTextExtractor, PdfPigTextExtractor>();

        services.AddHttpClient<OpenAiEmbeddingProvider>();
        services.AddScoped<IEmbeddingProvider>(sp =>
        {
            var embeddingOptions = sp.GetRequiredService<IOptions<EmbeddingOptions>>().Value;
            return embeddingOptions.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<OpenAiEmbeddingProvider>()
                : ActivatorUtilities.CreateInstance<MockEmbeddingProvider>(sp);
        });

        services.AddHttpClient<OpenAiLlmProvider>();
        services.AddScoped<ILlmProvider>(sp =>
        {
            var llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            return llmOptions.Provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<OpenAiLlmProvider>()
                : ActivatorUtilities.CreateInstance<MockLlmProvider>(sp);
        });

        services.AddHostedService<DocumentIndexingBackgroundService>();

        return services;
    }
}
