using DocuMind.Application.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DocuMind.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<UploadOptions>(configuration.GetSection(UploadOptions.SectionName));
        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }
}
