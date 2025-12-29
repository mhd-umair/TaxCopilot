namespace TaxCopilot.Domain.Entities;

/// <summary>
/// Represents extracted text from a single page of a document.
/// </summary>
public class PageText
{
    public int PageNumber { get; set; }
    public string Text { get; set; } = string.Empty;
}

