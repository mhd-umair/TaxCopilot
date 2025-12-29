using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Application.Services;
using TaxCopilot.Infrastructure.Azure;
using TaxCopilot.Infrastructure.Data;
using TaxCopilot.Infrastructure.OpenAI;
using TaxCopilot.Infrastructure.Storage;
using TaxCopilot.Infrastructure.TextExtraction;

namespace TaxCopilot.Infrastructure;

/// <summary>
/// Extension methods for registering infrastructure services.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration - OpenAI
        services.Configure<OpenAIOptions>(configuration.GetSection(OpenAIOptions.SectionName));
        
        // Configuration - Search
        services.Configure<AzureSearchOptions>(configuration.GetSection(AzureSearchOptions.SectionName));
        
        // Configuration - Storage
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<AzureBlobOptions>(configuration.GetSection(AzureBlobOptions.SectionName));
        services.Configure<LocalStorageOptions>(configuration.GetSection(LocalStorageOptions.SectionName));
        
        // Configuration - RAG
        services.Configure<RagOptions>(configuration.GetSection(RagOptions.SectionName));

        // Log warning if embedding dimensions not configured
        var embeddingDimensions = configuration.GetValue<int?>($"{OpenAIOptions.SectionName}:EmbeddingDimensions");
        if (!embeddingDimensions.HasValue)
        {
            Console.WriteLine("WARNING: OpenAI:EmbeddingDimensions not configured, defaulting to 1536");
        }

        // Data access (Dapper)
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();

        // Storage service - choose between Azure Blob or Local File storage
        var storageProvider = configuration.GetValue<string>($"{StorageOptions.SectionName}:Provider") ?? "Local";
        
        if (storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("INFO: Using Local File Storage");
            services.AddSingleton<IBlobStorageService, LocalFileStorageService>();
        }
        else
        {
            Console.WriteLine("INFO: Using Azure Blob Storage");
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }

        // OpenAI services
        Console.WriteLine("INFO: Using OpenAI API");
        services.AddSingleton<IEmbeddingService, OpenAIEmbeddingService>();
        services.AddSingleton<IChatService, OpenAIChatService>();

        // Azure AI Search (required for vector search)
        services.AddSingleton<ISearchService, AzureSearchService>();

        // Text extraction
        services.AddSingleton<IPdfTextExtractor, PdfTextExtractor>();
        services.AddSingleton<IDocxTextExtractor, DocxTextExtractor>();
        services.AddSingleton<ITextExtractor, CompositeTextExtractor>();

        // Application services
        services.AddScoped<IChunkingService, ChunkingService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<IngestionService>();
        services.AddScoped<RagService>();

        return services;
    }
}
