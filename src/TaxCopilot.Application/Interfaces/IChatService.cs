using TaxCopilot.Application.DTOs;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Interface for chat completion operations.
/// </summary>
public interface IChatService
{
    Task<AskResponse> GenerateAnswerAsync(
        string question,
        List<RetrievedChunk> context,
        CancellationToken cancellationToken = default);
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

