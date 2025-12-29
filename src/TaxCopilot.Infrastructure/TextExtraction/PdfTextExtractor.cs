using Microsoft.Extensions.Logging;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace TaxCopilot.Infrastructure.TextExtraction;

/// <summary>
/// PDF text extractor using UglyToad.PdfPig.
/// </summary>
public class PdfTextExtractor : IPdfTextExtractor
{
    private readonly ILogger<PdfTextExtractor> _logger;

    public PdfTextExtractor(ILogger<PdfTextExtractor> logger)
    {
        _logger = logger;
    }

    public Task<List<PageText>> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var pages = new List<PageText>();

        try
        {
            // Copy stream to memory since PdfPig needs seekable stream
            using var memoryStream = new MemoryStream();
            content.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var document = PdfDocument.Open(memoryStream);

            _logger.LogInformation("Extracting text from PDF with {PageCount} pages", document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var text = ExtractPageText(page);

                if (!string.IsNullOrWhiteSpace(text))
                {
                    pages.Add(new PageText
                    {
                        PageNumber = page.Number,
                        Text = text
                    });
                }
            }

            _logger.LogInformation("Extracted text from {ExtractedPages}/{TotalPages} pages", pages.Count, document.NumberOfPages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from PDF");
            throw;
        }

        return Task.FromResult(pages);
    }

    private string ExtractPageText(Page page)
    {
        try
        {
            // Get all words and join them with proper spacing
            var words = page.GetWords();
            var text = string.Join(" ", words.Select(w => w.Text));

            // Clean up the text
            text = CleanText(text);

            return text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract text from page {PageNumber}", page.Number);
            return string.Empty;
        }
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Replace multiple spaces with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        // Replace multiple newlines with double newline
        text = System.Text.RegularExpressions.Regex.Replace(text, @"(\r?\n){3,}", "\n\n");

        return text.Trim();
    }
}

