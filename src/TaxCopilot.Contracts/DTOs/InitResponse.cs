namespace TaxCopilot.Contracts.DTOs;

public class InitResponse
{
    public bool Success { get; set; }
    public bool BlobContainerCreated { get; set; }
    public bool SearchIndexCreated { get; set; }
    public string? Message { get; set; }
}

