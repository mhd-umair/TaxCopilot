namespace TaxCopilot.Domain.Entities;

/// <summary>
/// Represents a tax document stored in the system.
/// </summary>
public class Document
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Jurisdiction { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DocumentStatus Status { get; set; }
    public int? ChunkCount { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
}

/// <summary>
/// Document processing status.
/// </summary>
public enum DocumentStatus
{
    Uploaded = 0,
    Processing = 1,
    Indexed = 2,
    Failed = 3
}

