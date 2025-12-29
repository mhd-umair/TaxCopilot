namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Response DTO for document upload.
/// </summary>
public class DocumentUploadResponse
{
    public Guid DocumentId { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}

