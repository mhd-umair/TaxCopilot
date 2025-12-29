namespace TaxCopilot.Domain.Entities;

/// <summary>
/// Represents a chunk of text extracted from a document for indexing.
/// </summary>
public class TextChunk
{
    public string ChunkId { get; set; } = string.Empty;
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string ChunkText { get; set; } = string.Empty;
    public int PageNumberStart { get; set; }
    public int PageNumberEnd { get; set; }
    public string? SectionHeading { get; set; }
    public string Jurisdiction { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveDate { get; set; }
    public float[]? Embedding { get; set; }
}

