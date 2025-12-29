using Microsoft.Extensions.Logging;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Infrastructure.TextExtraction;

/// <summary>
/// Composite text extractor that routes to the appropriate extractor based on file type.
/// </summary>
public class CompositeTextExtractor : ITextExtractor
{
    private readonly IPdfTextExtractor _pdfExtractor;
    private readonly IDocxTextExtractor _docxExtractor;
    private readonly ILogger<CompositeTextExtractor> _logger;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx"
    };

    public CompositeTextExtractor(
        IPdfTextExtractor pdfExtractor,
        IDocxTextExtractor docxExtractor,
        ILogger<CompositeTextExtractor> logger)
    {
        _pdfExtractor = pdfExtractor;
        _docxExtractor = docxExtractor;
        _logger = logger;
    }

    public async Task<List<PageText>> ExtractAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        _logger.LogInformation("Extracting text from file: {FileName} (type: {Extension})", fileName, extension);

        return extension switch
        {
            ".pdf" => await _pdfExtractor.ExtractAsync(content, cancellationToken),
            ".docx" => await _docxExtractor.ExtractAsync(content, cancellationToken),
            _ => throw new NotSupportedException($"Unsupported file format: {extension}")
        };
    }

    public bool SupportsFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return SupportedExtensions.Contains(extension);
    }
}

