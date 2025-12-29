using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Infrastructure.Azure;

/// <summary>
/// Azure AI Search service implementation.
/// </summary>
public class AzureSearchService : ISearchService
{
    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private readonly int _embeddingDimensions;
    private readonly ILogger<AzureSearchService> _logger;

    private const string VectorSearchProfileName = "tax-vector-profile";
    private const string VectorSearchConfigName = "tax-hnsw-config";

    public AzureSearchService(
        IOptions<AzureSearchOptions> searchOptions,
        IOptions<OpenAIOptions> openAIOptions,
        ILogger<AzureSearchService> logger)
    {
        var options = searchOptions.Value;
        _indexName = options.IndexName;
        _embeddingDimensions = openAIOptions.Value.EmbeddingDimensions;
        _logger = logger;

        if (string.IsNullOrEmpty(options.Endpoint))
        {
            throw new InvalidOperationException("Azure Search endpoint not configured");
        }

        var credential = new AzureKeyCredential(options.Key);
        _indexClient = new SearchIndexClient(new Uri(options.Endpoint), credential);
        _searchClient = new SearchClient(new Uri(options.Endpoint), _indexName, credential);
    }

    public async Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if index exists
            try
            {
                await _indexClient.GetIndexAsync(_indexName, cancellationToken);
                _logger.LogInformation("Search index already exists: {IndexName}", _indexName);
                return false;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                // Index doesn't exist, create it
            }

            // Create index with vector search configuration
            var index = CreateIndexDefinition();

            await _indexClient.CreateIndexAsync(index, cancellationToken);
            _logger.LogInformation("Created search index: {IndexName}", _indexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure search index exists: {IndexName}", _indexName);
            throw;
        }
    }

    public async Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _indexClient.GetIndexAsync(_indexName, cancellationToken);
            return true;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check if index exists: {IndexName}", _indexName);
            return false;
        }
    }

    private SearchIndex CreateIndexDefinition()
    {
        var vectorSearch = new VectorSearch();

        vectorSearch.Algorithms.Add(new HnswAlgorithmConfiguration(VectorSearchConfigName)
        {
            Parameters = new HnswParameters
            {
                Metric = VectorSearchAlgorithmMetric.Cosine,
                M = 4,
                EfConstruction = 400,
                EfSearch = 500
            }
        });

        vectorSearch.Profiles.Add(new VectorSearchProfile(VectorSearchProfileName, VectorSearchConfigName));

        var index = new SearchIndex(_indexName)
        {
            VectorSearch = vectorSearch,
            Fields = new List<SearchField>
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("documentId", SearchFieldDataType.String) { IsFilterable = true },
                new SearchableField("documentTitle") { IsFilterable = true },
                new SearchableField("chunkText") { AnalyzerName = LexicalAnalyzerName.EnMicrosoft },
                new SimpleField("jurisdiction", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("taxType", SearchFieldDataType.String) { IsFilterable = true, IsFacetable = true },
                new SimpleField("version", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("effectiveDate", SearchFieldDataType.DateTimeOffset) { IsFilterable = true, IsSortable = true },
                new SimpleField("pageNumber", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true },
                new SearchableField("sectionHeading") { IsFilterable = true },
                new SearchField("chunkEmbedding", SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    IsSearchable = true,
                    VectorSearchDimensions = _embeddingDimensions,
                    VectorSearchProfileName = VectorSearchProfileName
                }
            }
        };

        return index;
    }

    public async Task<int> IndexChunksAsync(List<TextChunk> chunks, CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            return 0;
        }

        const int batchSize = 100;
        int indexedCount = 0;

        for (int i = 0; i < chunks.Count; i += batchSize)
        {
            var batch = chunks.Skip(i).Take(batchSize).ToList();

            var documents = batch.Select(chunk => new SearchDocument
            {
                ["id"] = chunk.ChunkId,
                ["documentId"] = chunk.DocumentId.ToString(),
                ["documentTitle"] = chunk.DocumentTitle,
                ["chunkText"] = chunk.ChunkText,
                ["jurisdiction"] = chunk.Jurisdiction,
                ["taxType"] = chunk.TaxType,
                ["version"] = chunk.Version,
                ["effectiveDate"] = chunk.EffectiveDate,
                ["pageNumber"] = chunk.PageNumberStart,
                ["sectionHeading"] = chunk.SectionHeading ?? string.Empty,
                ["chunkEmbedding"] = chunk.Embedding
            }).ToList();

            try
            {
                var response = await _searchClient.MergeOrUploadDocumentsAsync(documents, cancellationToken: cancellationToken);
                indexedCount += response.Value.Results.Count(r => r.Succeeded);

                _logger.LogDebug("Indexed batch {BatchIndex}/{TotalBatches}: {SuccessCount} documents",
                    (i / batchSize) + 1, (chunks.Count + batchSize - 1) / batchSize,
                    response.Value.Results.Count(r => r.Succeeded));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index batch {BatchIndex}", i / batchSize);
                throw;
            }
        }

        _logger.LogInformation("Indexed {IndexedCount}/{TotalCount} chunks", indexedCount, chunks.Count);
        return indexedCount;
    }

    public async Task<List<RetrievedChunk>> SearchAsync(
        string query,
        float[] queryEmbedding,
        QueryFilters? filters,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var vectorQuery = new VectorizedQuery(queryEmbedding)
        {
            KNearestNeighborsCount = topK,
            Fields = { "chunkEmbedding" }
        };

        var searchOptions = new SearchOptions
        {
            Size = topK,
            Select = { "id", "documentId", "documentTitle", "chunkText", "pageNumber", "sectionHeading" },
            VectorSearch = new VectorSearchOptions
            {
                Queries = { vectorQuery }
            },
            QueryType = SearchQueryType.Simple,
            
        };

        // Build OData filter
        var filterParts = new List<string>();
        if (!string.IsNullOrEmpty(filters?.Jurisdiction))
        {
            filterParts.Add($"jurisdiction eq '{EscapeODataValue(filters.Jurisdiction)}'");
        }
        if (!string.IsNullOrEmpty(filters?.TaxType))
        {
            filterParts.Add($"taxType eq '{EscapeODataValue(filters.TaxType)}'");
        }
        if (!string.IsNullOrEmpty(filters?.Version))
        {
            filterParts.Add($"version eq '{EscapeODataValue(filters.Version)}'");
        }

        if (filterParts.Count > 0)
        {
            searchOptions.Filter = string.Join(" and ", filterParts);
        }

        try
        {
            // Perform hybrid search (vector + keyword)
            var response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions, cancellationToken);

            var results = new List<RetrievedChunk>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Add(new RetrievedChunk
                {
                    ChunkId = result.Document["id"]?.ToString() ?? string.Empty,
                    DocumentId = result.Document["documentId"]?.ToString() ?? string.Empty,
                    DocumentTitle = result.Document["documentTitle"]?.ToString() ?? string.Empty,
                    ChunkText = result.Document["chunkText"]?.ToString() ?? string.Empty,
                    PageNumber = result.Document["pageNumber"] is int pageNum ? pageNum : 0,
                    SectionHeading = result.Document["sectionHeading"]?.ToString(),
                    Score = result.Score ?? 0
                });
            }

            _logger.LogInformation("Search returned {ResultCount} results", results.Count);
            return results;
        }
        catch (RequestFailedException ex) when (ex.Message.Contains("semantic"))
        {
            // Fallback: Semantic search not available, use vector-only search
            _logger.LogWarning("Semantic search not available, falling back to vector search");

            searchOptions.QueryType = SearchQueryType.Simple;
            searchOptions.SemanticSearch = null;

            var response = await _searchClient.SearchAsync<SearchDocument>(query, searchOptions, cancellationToken);

            var results = new List<RetrievedChunk>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                results.Add(new RetrievedChunk
                {
                    ChunkId = result.Document["id"]?.ToString() ?? string.Empty,
                    DocumentId = result.Document["documentId"]?.ToString() ?? string.Empty,
                    DocumentTitle = result.Document["documentTitle"]?.ToString() ?? string.Empty,
                    ChunkText = result.Document["chunkText"]?.ToString() ?? string.Empty,
                    PageNumber = result.Document["pageNumber"] is int pageNum ? pageNum : 0,
                    SectionHeading = result.Document["sectionHeading"]?.ToString(),
                    Score = result.Score ?? 0
                });
            }

            return results;
        }
    }

    public async Task DeleteDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Search for all chunks belonging to this document
            var searchOptions = new SearchOptions
            {
                Filter = $"documentId eq '{documentId}'",
                Select = { "id" },
                Size = 1000
            };

            var response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions, cancellationToken);

            var keysToDelete = new List<string>();
            await foreach (var result in response.Value.GetResultsAsync())
            {
                var id = result.Document["id"]?.ToString();
                if (!string.IsNullOrEmpty(id))
                {
                    keysToDelete.Add(id);
                }
            }

            if (keysToDelete.Count > 0)
            {
                var batch = IndexDocumentsBatch.Delete("id", keysToDelete);
                await _searchClient.IndexDocumentsAsync(batch, cancellationToken: cancellationToken);
                _logger.LogInformation("Deleted {Count} chunks for document {DocumentId}", keysToDelete.Count, documentId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete chunks for document {DocumentId}", documentId);
            throw;
        }
    }

    private static string EscapeODataValue(string value)
    {
        return value.Replace("'", "''");
    }
}

