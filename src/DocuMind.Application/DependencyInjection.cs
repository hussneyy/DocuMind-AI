using DocuMind.Application.Documents;
using DocuMind.Application.Embeddings;
using DocuMind.Application.Search;
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
        services.Configure<EmbeddingOptions>(configuration.GetSection(EmbeddingOptions.SectionName));
        services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));

        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentIndexer, DocumentIndexer>();
        services.AddScoped<ISearchService, SearchService>();

        return services;
    }
}
