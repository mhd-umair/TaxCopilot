namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Response DTO for admin init endpoint.
/// </summary>
public class InitResponse
{
    public bool Success { get; set; }
    public bool BlobContainerCreated { get; set; }
    public bool SearchIndexCreated { get; set; }
    public string? Message { get; set; }
}

