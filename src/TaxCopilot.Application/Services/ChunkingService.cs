using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Services;

/// <summary>
/// Service for chunking document text with page preservation.
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly RagOptions _options;
    private readonly ILogger<ChunkingService> _logger;

    // Regex patterns for detecting section headings
    private static readonly Regex SectionHeadingPattern = new(
        @"^(?:Chapter|Section|Part|Article|§)\s*[\d\.]+[:\.\s].*$|^[\d\.]+\s+[A-Z][^\.]{10,80}$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    public ChunkingService(IOptions<RagOptions> options, ILogger<ChunkingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public List<TextChunk> ChunkDocument(
        List<PageText> pages,
        Guid documentId,
        string documentTitle,
        string jurisdiction,
        string taxType,
        string version,
        DateTimeOffset? effectiveDate)
    {
        var chunks = new List<TextChunk>();

        if (pages.Count == 0)
        {
            _logger.LogWarning("No pages to chunk for document {DocumentId}", documentId);
            return chunks;
        }

        var currentChunk = new StringBuilder();
        int currentChunkStart = pages[0].PageNumber;
        int currentChunkEnd = currentChunkStart;
        string? currentSectionHeading = null;

        foreach (var page in pages)
        {
            var pageText = page.Text.Trim();
            if (string.IsNullOrWhiteSpace(pageText))
            {
                continue;
            }

            // Detect section heading at the start of the page
            var detectedHeading = DetectSectionHeading(pageText);
            if (detectedHeading != null)
            {
                currentSectionHeading = detectedHeading;
            }

            // Check if adding this page would exceed chunk size
            if (currentChunk.Length > 0 && currentChunk.Length + pageText.Length > _options.ChunkSizeChars)
            {
                // Finalize current chunk
                chunks.Add(CreateChunk(
                    currentChunk.ToString(),
                    documentId,
                    documentTitle,
                    currentChunkStart,
                    currentChunkEnd,
                    currentSectionHeading,
                    jurisdiction,
                    taxType,
                    version,
                    effectiveDate));

                // Start new chunk with overlap
                var overlap = GetOverlapText(currentChunk.ToString());
                currentChunk.Clear();
                currentChunk.Append(overlap);
                currentChunkStart = page.PageNumber;
            }

            // Check if a single page is too large - split within page
            if (pageText.Length > _options.ChunkSizeChars)
            {
                // Finalize any existing chunk content first
                if (currentChunk.Length > 0)
                {
                    chunks.Add(CreateChunk(
                        currentChunk.ToString(),
                        documentId,
                        documentTitle,
                        currentChunkStart,
                        currentChunkEnd,
                        currentSectionHeading,
                        jurisdiction,
                        taxType,
                        version,
                        effectiveDate));
                    currentChunk.Clear();
                }

                // Split the large page into multiple chunks
                var pageChunks = SplitLargeText(pageText, page.PageNumber);
                foreach (var pageChunk in pageChunks)
                {
                    chunks.Add(CreateChunk(
                        pageChunk,
                        documentId,
                        documentTitle,
                        page.PageNumber,
                        page.PageNumber,
                        currentSectionHeading,
                        jurisdiction,
                        taxType,
                        version,
                        effectiveDate));
                }

                currentChunkStart = page.PageNumber + 1;
            }
            else
            {
                // Add page text to current chunk
                if (currentChunk.Length > 0)
                {
                    currentChunk.Append("\n\n");
                }
                currentChunk.Append(pageText);
                currentChunkEnd = page.PageNumber;
            }
        }

        // Finalize last chunk
        if (currentChunk.Length > 0)
        {
            chunks.Add(CreateChunk(
                currentChunk.ToString(),
                documentId,
                documentTitle,
                currentChunkStart,
                currentChunkEnd,
                currentSectionHeading,
                jurisdiction,
                taxType,
                version,
                effectiveDate));
        }

        _logger.LogInformation("Created {ChunkCount} chunks from {PageCount} pages", chunks.Count, pages.Count);

        return chunks;
    }

    private TextChunk CreateChunk(
        string text,
        Guid documentId,
        string documentTitle,
        int pageStart,
        int pageEnd,
        string? sectionHeading,
        string jurisdiction,
        string taxType,
        string version,
        DateTimeOffset? effectiveDate)
    {
        return new TextChunk
        {
            ChunkId = Guid.NewGuid().ToString(),
            DocumentId = documentId,
            DocumentTitle = documentTitle,
            ChunkText = text.Trim(),
            PageNumberStart = pageStart,
            PageNumberEnd = pageEnd,
            SectionHeading = sectionHeading,
            Jurisdiction = jurisdiction,
            TaxType = taxType,
            Version = version,
            EffectiveDate = effectiveDate
        };
    }

    private string GetOverlapText(string text)
    {
        if (text.Length <= _options.ChunkOverlapChars)
        {
            return text;
        }

        // Try to find a sentence boundary in the overlap region
        var overlapStart = text.Length - _options.ChunkOverlapChars;
        var overlapText = text.Substring(overlapStart);

        // Look for sentence boundary
        var sentenceEnd = overlapText.IndexOfAny(new[] { '.', '!', '?' });
        if (sentenceEnd > 0 && sentenceEnd < overlapText.Length - 10)
        {
            return overlapText.Substring(sentenceEnd + 1).TrimStart();
        }

        // Look for paragraph boundary
        var paragraphEnd = overlapText.IndexOf("\n\n");
        if (paragraphEnd > 0)
        {
            return overlapText.Substring(paragraphEnd + 2);
        }

        return overlapText;
    }

    private List<string> SplitLargeText(string text, int pageNumber)
    {
        var chunks = new List<string>();
        var targetSize = _options.ChunkSizeChars;
        var overlap = _options.ChunkOverlapChars;

        int currentPos = 0;
        while (currentPos < text.Length)
        {
            var remainingLength = text.Length - currentPos;
            var chunkLength = Math.Min(targetSize, remainingLength);

            // Try to find a good break point
            if (chunkLength < remainingLength)
            {
                var breakPoint = FindBreakPoint(text, currentPos, currentPos + chunkLength);
                if (breakPoint > currentPos)
                {
                    chunkLength = breakPoint - currentPos;
                }
            }

            chunks.Add(text.Substring(currentPos, chunkLength));

            // Move position, accounting for overlap
            currentPos += chunkLength;
            if (currentPos < text.Length)
            {
                currentPos = Math.Max(currentPos - overlap, currentPos - chunkLength + 100);
            }
        }

        return chunks;
    }

    private int FindBreakPoint(string text, int start, int end)
    {
        // Look backwards from end to find a sentence boundary
        for (int i = end - 1; i >= start + (end - start) / 2; i--)
        {
            if (text[i] == '.' || text[i] == '!' || text[i] == '?')
            {
                return i + 1;
            }
        }

        // Look for newline
        for (int i = end - 1; i >= start + (end - start) / 2; i--)
        {
            if (text[i] == '\n')
            {
                return i + 1;
            }
        }

        // Look for space
        for (int i = end - 1; i >= start + (end - start) * 3 / 4; i--)
        {
            if (text[i] == ' ')
            {
                return i + 1;
            }
        }

        return end;
    }

    private string? DetectSectionHeading(string text)
    {
        var match = SectionHeadingPattern.Match(text);
        if (match.Success)
        {
            var heading = match.Value.Trim();
            // Limit heading length
            if (heading.Length > 100)
            {
                heading = heading.Substring(0, 100) + "...";
            }
            return heading;
        }
        return null;
    }
}

