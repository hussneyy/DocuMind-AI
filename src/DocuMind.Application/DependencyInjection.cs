using DocuMind.Application.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));
        services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));
        services.Configure<IndexingOptions>(configuration.GetSection(IndexingOptions.SectionName));

        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentIndexer, DocumentIndexer>();

        return services;
    }
}
