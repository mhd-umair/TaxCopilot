using Microsoft.AspNetCore.Http;

namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Request DTO for document upload.
/// </summary>
public class DocumentUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = string.Empty;
    public string TaxType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public DateTimeOffset? EffectiveDate { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
}

