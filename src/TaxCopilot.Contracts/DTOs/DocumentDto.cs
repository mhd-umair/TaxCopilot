namespace TaxCopilot.Contracts.DTOs;

public class DocumentDto
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
    public int Status { get; set; }
    public int? ChunkCount { get; set; }
    public DateTimeOffset? IndexedAt { get; set; }
    
    public string StatusText => Status switch
    {
        0 => "Uploaded",
        1 => "Processing",
        2 => "Indexed",
        3 => "Failed",
        _ => "Unknown"
    };
}

