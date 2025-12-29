namespace TaxCopilot.Domain.Entities;

/// <summary>
/// Represents an audit log entry for RAG queries.
/// </summary>
public class AuditLog
{
    public Guid AuditLogId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string QueryText { get; set; } = string.Empty;
    public string? FiltersJson { get; set; }
    public string? RetrievedChunksJson { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string? AnswerText { get; set; }
    public int LatencyMs { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

