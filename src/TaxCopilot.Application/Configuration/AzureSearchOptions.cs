namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for Azure AI Search.
/// </summary>
public class AzureSearchOptions
{
    public const string SectionName = "AzureSearch";

    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string IndexName { get; set; } = "tax-documents";
}

