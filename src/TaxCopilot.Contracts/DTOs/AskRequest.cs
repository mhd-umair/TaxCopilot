namespace TaxCopilot.Contracts.DTOs;

public class AskRequest
{
    public string Question { get; set; } = string.Empty;
    public QueryFilters? Filters { get; set; }
}

public class QueryFilters
{
    public string? Jurisdiction { get; set; }
    public string? TaxType { get; set; }
    public string? Version { get; set; }
}

