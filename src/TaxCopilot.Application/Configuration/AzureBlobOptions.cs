namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for Azure Blob Storage.
/// </summary>
public class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";

    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "tax-docs";
}

