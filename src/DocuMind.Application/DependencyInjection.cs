using DocuMind.Application.Chat;
using DocuMind.Application.Documents;
using DocuMind.Application.Embeddings;
using DocuMind.Application.Llm;
using DocuMind.Application.Search;
using DocuMind.Application.Summaries;
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
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.SectionName));
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));
        services.Configure<SummaryOptions>(configuration.GetSection(SummaryOptions.SectionName));

        services.AddScoped<IDocumentService, DocumentService>();
        services.AddSingleton<ITextChunker, TextChunker>();
        services.AddScoped<IDocumentIndexer, DocumentIndexer>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IRagAnswerGenerator, RagAnswerGenerator>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<ISummaryService, SummaryService>();

        return services;
    }
}
