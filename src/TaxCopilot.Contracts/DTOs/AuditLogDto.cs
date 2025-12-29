namespace TaxCopilot.Contracts.DTOs;

public class AuditLogDto
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
    
    public string QuestionSnippet => QueryText.Length > 60 
        ? QueryText.Substring(0, 60) + "..." 
        : QueryText;
}

