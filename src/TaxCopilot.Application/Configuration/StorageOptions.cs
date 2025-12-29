namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for storage provider selection.
/// </summary>
public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider type: "Azure" or "Local". Defaults to "Azure".
    /// </summary>
    public string Provider { get; set; } = "Azure";

    /// <summary>
    /// Whether to use Azure Blob Storage.
    /// </summary>
    public bool UseAzure => Provider.Equals("Azure", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Whether to use local file storage.
    /// </summary>
    public bool UseLocal => Provider.Equals("Local", StringComparison.OrdinalIgnoreCase);
}

