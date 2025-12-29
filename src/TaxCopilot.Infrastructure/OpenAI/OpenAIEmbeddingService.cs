using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Embeddings;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.Interfaces;

namespace TaxCopilot.Infrastructure.OpenAI;

/// <summary>
/// OpenAI embedding service implementation (non-Azure).
/// </summary>
public class OpenAIEmbeddingService : IEmbeddingService
{
    private readonly EmbeddingClient _embeddingClient;
    private readonly int _dimensions;
    private readonly ILogger<OpenAIEmbeddingService> _logger;

    public OpenAIEmbeddingService(IOptions<OpenAIOptions> options, ILogger<OpenAIEmbeddingService> logger)
    {
        var openAIOptions = options.Value;
        _dimensions = openAIOptions.EmbeddingDimensions;
        _logger = logger;

        if (string.IsNullOrEmpty(openAIOptions.ApiKey))
        {
            throw new InvalidOperationException("OpenAI API key not configured");
        }

        var client = new OpenAIClient(openAIOptions.ApiKey);
        _embeddingClient = client.GetEmbeddingClient(openAIOptions.EmbeddingModel);

        _logger.LogInformation("OpenAI Embedding service initialized with model: {Model}", openAIOptions.EmbeddingModel);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new float[_dimensions];
        }

        try
        {
            var embeddingOptions = new EmbeddingGenerationOptions
            {
                Dimensions = _dimensions
            };

            var response = await _embeddingClient.GenerateEmbeddingAsync(text, embeddingOptions, cancellationToken);

            return response.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate embedding");
            throw;
        }
    }

    public async Task<List<float[]>> GenerateEmbeddingsAsync(List<string> texts, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return new List<float[]>();
        }

        var results = new List<float[]>();
        const int batchSize = 16;

        for (int i = 0; i < texts.Count; i += batchSize)
        {
            var batch = texts.Skip(i).Take(batchSize).ToList();

            try
            {
                var embeddingOptions = new EmbeddingGenerationOptions
                {
                    Dimensions = _dimensions
                };

                var response = await _embeddingClient.GenerateEmbeddingsAsync(batch, embeddingOptions, cancellationToken);

                foreach (var embedding in response.Value)
                {
                    results.Add(embedding.ToFloats().ToArray());
                }

                _logger.LogDebug("Generated embeddings for batch {BatchIndex}/{TotalBatches}",
                    (i / batchSize) + 1, (texts.Count + batchSize - 1) / batchSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate embeddings for batch {BatchIndex}", i / batchSize);
                throw;
            }
        }

        return results;
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testEmbedding = await GenerateEmbeddingAsync("test", cancellationToken);
            return testEmbedding.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI Embedding service not available");
            return false;
        }
    }
}

