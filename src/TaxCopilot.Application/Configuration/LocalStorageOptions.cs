namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for local file storage.
/// </summary>
public class LocalStorageOptions
{
    public const string SectionName = "LocalStorage";

    /// <summary>
    /// Base path for storing files. Defaults to "./storage" relative to the app.
    /// </summary>
    public string BasePath { get; set; } = "./storage";

    /// <summary>
    /// Subfolder name for documents. Defaults to "documents".
    /// </summary>
    public string DocumentsFolder { get; set; } = "documents";
}

