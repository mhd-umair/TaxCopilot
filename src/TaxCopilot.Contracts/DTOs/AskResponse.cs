using System.Text.Json.Serialization;

namespace TaxCopilot.Contracts.DTOs;

public class AskResponse
{
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("citations")]
    public List<Citation> Citations { get; set; } = new();

    [JsonPropertyName("confidence")]
    public string Confidence { get; set; } = "low";
}

public class Citation
{
    [JsonPropertyName("documentTitle")]
    public string DocumentTitle { get; set; } = string.Empty;

    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("sectionHeading")]
    public string? SectionHeading { get; set; }

    [JsonPropertyName("chunkId")]
    public string ChunkId { get; set; } = string.Empty;
}

