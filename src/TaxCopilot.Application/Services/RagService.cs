using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Services;

/// <summary>
/// Service for RAG (Retrieval-Augmented Generation) operations.
/// </summary>
public class RagService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly ISearchService _searchService;
    private readonly IChatService _chatService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly RagOptions _ragOptions;
    private readonly OpenAIOptions _openAIOptions;
    private readonly ILogger<RagService> _logger;

    private const string PromptVersion = "v1.0";

    public RagService(
        IEmbeddingService embeddingService,
        ISearchService searchService,
        IChatService chatService,
        IAuditLogRepository auditLogRepository,
        IOptions<RagOptions> ragOptions,
        IOptions<OpenAIOptions> openAIOptions,
        ILogger<RagService> logger)
    {
        _embeddingService = embeddingService;
        _searchService = searchService;
        _chatService = chatService;
        _auditLogRepository = auditLogRepository;
        _ragOptions = ragOptions.Value;
        _openAIOptions = openAIOptions.Value;
        _logger = logger;
    }

    public async Task<AskResponse> AskAsync(
        string question,
        QueryFilters? filters,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Processing question: {Question}", question);

        AskResponse? response = null;
        string? errorMessage = null;
        List<RetrievedChunk>? retrievedChunks = null;

        try
        {
            // Generate query embedding
            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(question, cancellationToken);

            // Search for relevant chunks
            retrievedChunks = await _searchService.SearchAsync(
                question,
                queryEmbedding,
                filters,
                _ragOptions.TopK,
                cancellationToken);

            _logger.LogInformation("Retrieved {ChunkCount} chunks for question", retrievedChunks.Count);

            // Take top N chunks for context
            var contextChunks = retrievedChunks.Take(_ragOptions.ContextChunks).ToList();

            // Generate answer
            response = await _chatService.GenerateAnswerAsync(question, contextChunks, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Question answered in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            _logger.LogError(ex, "Error processing question");
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Persist audit log
            var auditLog = new AuditLog
            {
                AuditLogId = Guid.NewGuid(),
                CorrelationId = correlationId,
                QueryText = question,
                FiltersJson = filters != null ? JsonSerializer.Serialize(filters) : null,
                RetrievedChunksJson = retrievedChunks != null
                    ? JsonSerializer.Serialize(retrievedChunks.Select(c => new
                    {
                        c.ChunkId,
                        c.Score,
                        c.DocumentTitle,
                        c.PageNumber
                    }))
                    : null,
                Model = _openAIOptions.ChatModel,
                PromptVersion = PromptVersion,
                AnswerText = response?.Answer,
                LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                ErrorMessage = errorMessage,
                CreatedAt = DateTimeOffset.UtcNow
            };

            try
            {
                await _auditLogRepository.CreateAsync(auditLog, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist audit log");
            }
        }
    }
}
