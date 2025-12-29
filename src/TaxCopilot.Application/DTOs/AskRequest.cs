namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Request DTO for RAG chat query.
/// </summary>
public class AskRequest
{
    public string Question { get; set; } = string.Empty;
    public QueryFilters? Filters { get; set; }
}

/// <summary>
/// Filters for RAG query.
/// </summary>
public class QueryFilters
{
    public string? Jurisdiction { get; set; }
    public string? TaxType { get; set; }
    public string? Version { get; set; }
}

