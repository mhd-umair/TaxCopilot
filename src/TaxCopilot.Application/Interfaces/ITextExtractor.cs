using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Interface for extracting text from documents.
/// </summary>
public interface ITextExtractor
{
    Task<List<PageText>> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    bool SupportsFileType(string fileName);
}

/// <summary>
/// Interface for PDF text extraction.
/// </summary>
public interface IPdfTextExtractor
{
    Task<List<PageText>> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interface for DOCX text extraction.
/// </summary>
public interface IDocxTextExtractor
{
    Task<List<PageText>> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}

