namespace TaxCopilot.Contracts.DTOs;

public class HealthCheckResponse
{
    public string Status { get; set; } = "Healthy";
    public Dictionary<string, ComponentHealth> Components { get; set; } = new();
}

public class ComponentHealth
{
    public bool Healthy { get; set; }
    public string? Message { get; set; }
}

