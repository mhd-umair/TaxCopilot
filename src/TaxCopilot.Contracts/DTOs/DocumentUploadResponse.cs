namespace TaxCopilot.Contracts.DTOs;

public class DocumentUploadResponse
{
    public Guid DocumentId { get; set; }
    public string BlobUrl { get; set; } = string.Empty;
}

