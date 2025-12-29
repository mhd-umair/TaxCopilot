using TaxCopilot.Application.DTOs;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Interface for Azure AI Search operations.
/// </summary>
public interface ISearchService
{
    Task<bool> EnsureIndexExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> IndexExistsAsync(CancellationToken cancellationToken = default);
    Task<int> IndexChunksAsync(List<TextChunk> chunks, CancellationToken cancellationToken = default);
    Task<List<RetrievedChunk>> SearchAsync(
        string query,
        float[] queryEmbedding,
        QueryFilters? filters,
        int topK,
        CancellationToken cancellationToken = default);
    Task DeleteDocumentChunksAsync(Guid documentId, CancellationToken cancellationToken = default);
}

