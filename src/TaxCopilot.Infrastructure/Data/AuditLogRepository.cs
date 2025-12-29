using Dapper;
using Microsoft.Extensions.Logging;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Infrastructure.Data;

/// <summary>
/// Dapper implementation of audit log repository.
/// </summary>
public class AuditLogRepository : IAuditLogRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<AuditLogRepository> _logger;

    public AuditLogRepository(ISqlConnectionFactory connectionFactory, ILogger<AuditLogRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO AuditLogs (AuditLogId, CorrelationId, QueryText, FiltersJson,
                                   RetrievedChunksJson, Model, PromptVersion, AnswerText,
                                   LatencyMs, ErrorMessage, CreatedAt)
            VALUES (@AuditLogId, @CorrelationId, @QueryText, @FiltersJson,
                    @RetrievedChunksJson, @Model, @PromptVersion, @AnswerText,
                    @LatencyMs, @ErrorMessage, @CreatedAt)";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, auditLog, cancellationToken: cancellationToken));

        _logger.LogDebug("Created audit log: {AuditLogId}", auditLog.AuditLogId);
        return auditLog.AuditLogId;
    }

    public async Task<IEnumerable<AuditLog>> GetRecentAsync(int count = 100, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT TOP (@Count) AuditLogId, CorrelationId, QueryText, FiltersJson,
                   RetrievedChunksJson, Model, PromptVersion, AnswerText,
                   LatencyMs, ErrorMessage, CreatedAt
            FROM AuditLogs
            ORDER BY CreatedAt DESC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<AuditLog>(
            new CommandDefinition(sql, new { Count = count }, cancellationToken: cancellationToken));
    }
}

