using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Interface for chunking extracted text.
/// </summary>
public interface IChunkingService
{
    List<TextChunk> ChunkDocument(
        List<PageText> pages,
        Guid documentId,
        string documentTitle,
        string jurisdiction,
        string taxType,
        string version,
        DateTimeOffset? effectiveDate);
}

