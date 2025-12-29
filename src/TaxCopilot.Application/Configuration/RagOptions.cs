namespace TaxCopilot.Application.Configuration;

/// <summary>
/// Configuration options for RAG processing.
/// </summary>
public class RagOptions
{
    public const string SectionName = "Rag";

    public int ChunkSizeChars { get; set; } = 3500;
    public int ChunkOverlapChars { get; set; } = 400;
    public int TopK { get; set; } = 12;
    public int ContextChunks { get; set; } = 8;
}

