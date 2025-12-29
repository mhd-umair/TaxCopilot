namespace TaxCopilot.Contracts.DTOs;

public class IngestResponse
{
    public Guid DocumentId { get; set; }
    public int ChunksCreated { get; set; }
    public int ChunksIndexed { get; set; }
    public long ElapsedMs { get; set; }
}

