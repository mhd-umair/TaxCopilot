using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Repository interface for document operations.
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Guid> CreateAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateAsync(Document document, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid documentId, DocumentStatus status, int? chunkCount = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}

