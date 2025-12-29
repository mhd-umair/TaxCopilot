namespace TaxCopilot.Application.DTOs;

/// <summary>
/// Response DTO for health check.
/// </summary>
public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

/// <summary>
/// Health status of a single component.
/// </summary>
public class ComponentHealth
{
    public bool Healthy { get; set; }
    public string? Message { get; set; }
}

