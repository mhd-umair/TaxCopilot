using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Logging;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Infrastructure.TextExtraction;

/// <summary>
/// DOCX text extractor using DocumentFormat.OpenXml.
/// </summary>
public class DocxTextExtractor : IDocxTextExtractor
{
    private readonly ILogger<DocxTextExtractor> _logger;

    public DocxTextExtractor(ILogger<DocxTextExtractor> logger)
    {
        _logger = logger;
    }

    public Task<List<PageText>> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var pages = new List<PageText>();

        try
        {
            // Copy stream to memory since OpenXml needs seekable stream
            using var memoryStream = new MemoryStream();
            content.CopyTo(memoryStream);
            memoryStream.Position = 0;

            using var document = WordprocessingDocument.Open(memoryStream, false);

            var body = document.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                _logger.LogWarning("DOCX document has no body content");
                return Task.FromResult(pages);
            }

            var textBuilder = new StringBuilder();
            int estimatedPage = 1;
            int charsSincePageBreak = 0;
            const int charsPerPage = 3000; // Approximate characters per page

            foreach (var element in body.Elements())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Check for page break
                if (element is Paragraph paragraph)
                {
                    var hasPageBreak = paragraph.Descendants<Break>()
                        .Any(b => b.Type?.Value == BreakValues.Page);

                    if (hasPageBreak && textBuilder.Length > 0)
                    {
                        // Save current page
                        pages.Add(new PageText
                        {
                            PageNumber = estimatedPage,
                            Text = textBuilder.ToString().Trim()
                        });

                        textBuilder.Clear();
                        estimatedPage++;
                        charsSincePageBreak = 0;
                    }

                    var paragraphText = GetParagraphText(paragraph);
                    if (!string.IsNullOrWhiteSpace(paragraphText))
                    {
                        textBuilder.AppendLine(paragraphText);
                        charsSincePageBreak += paragraphText.Length;

                        // Estimate page break based on character count
                        if (charsSincePageBreak > charsPerPage)
                        {
                            pages.Add(new PageText
                            {
                                PageNumber = estimatedPage,
                                Text = textBuilder.ToString().Trim()
                            });

                            textBuilder.Clear();
                            estimatedPage++;
                            charsSincePageBreak = 0;
                        }
                    }
                }
                else if (element is Table table)
                {
                    var tableText = GetTableText(table);
                    if (!string.IsNullOrWhiteSpace(tableText))
                    {
                        textBuilder.AppendLine(tableText);
                        charsSincePageBreak += tableText.Length;
                    }
                }
            }

            // Add remaining content as last page
            if (textBuilder.Length > 0)
            {
                pages.Add(new PageText
                {
                    PageNumber = estimatedPage,
                    Text = textBuilder.ToString().Trim()
                });
            }

            _logger.LogInformation("Extracted text from DOCX: {PageCount} estimated pages", pages.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract text from DOCX");
            throw;
        }

        return Task.FromResult(pages);
    }

    private static string GetParagraphText(Paragraph paragraph)
    {
        var text = paragraph.InnerText;
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text;
    }

    private static string GetTableText(Table table)
    {
        var builder = new StringBuilder();

        foreach (var row in table.Elements<TableRow>())
        {
            var cells = row.Elements<TableCell>().Select(c => c.InnerText.Trim());
            builder.AppendLine(string.Join(" | ", cells));
        }

        return builder.ToString();
    }
}

