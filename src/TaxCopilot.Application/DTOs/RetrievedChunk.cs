namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Represents a chunk retrieved from the search index.
/// </summary>
public class RetrievedChunk
{
    public string ChunkId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string ChunkText { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public string? SectionHeading { get; set; }
    public double Score { get; set; }
}

