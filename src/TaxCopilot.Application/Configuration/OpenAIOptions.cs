namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for OpenAI API (non-Azure).
/// </summary>
public class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// OpenAI API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Chat model name (e.g., "gpt-4o", "gpt-4-turbo", "gpt-3.5-turbo").
    /// </summary>
    public string ChatModel { get; set; } = "gpt-4o";

    /// <summary>
    /// Embedding model name (e.g., "text-embedding-3-small", "text-embedding-3-large").
    /// </summary>
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    /// <summary>
    /// Embedding dimensions. Must match the model's output dimensions.
    /// </summary>
    public int EmbeddingDimensions { get; set; } = 1536;
}

