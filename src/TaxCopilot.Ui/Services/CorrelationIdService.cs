namespace TaxCopilot.Ui.Services;

/// <summary>
/// Service to track correlation IDs across the UI.
/// </summary>
public class CorrelationIdService
{
    public string? LastCorrelationId { get; private set; }
    public DateTimeOffset? LastRequestTime { get; private set; }

    public event Action? OnChange;

    public string GenerateNew()
    {
        LastCorrelationId = Guid.NewGuid().ToString("N")[..12];
        LastRequestTime = DateTimeOffset.Now;
        NotifyStateChanged();
        return LastCorrelationId;
    }

    public void Set(string correlationId)
    {
        LastCorrelationId = correlationId;
        LastRequestTime = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

