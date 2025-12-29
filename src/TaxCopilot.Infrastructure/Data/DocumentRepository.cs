using Dapper;
using Microsoft.Extensions.Logging;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Infrastructure.Data;

/// <summary>
/// Dapper implementation of document repository.
/// </summary>
public class DocumentRepository : IDocumentRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ILogger<DocumentRepository> _logger;

    public DocumentRepository(ISqlConnectionFactory connectionFactory, ILogger<DocumentRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DocumentId, Title, FileName, BlobUrl, ContentType, FileSizeBytes,
                   Jurisdiction, TaxType, Version, EffectiveDate, UploadedBy,
                   CreatedAt, UpdatedAt, Status, ChunkCount, IndexedAt
            FROM Documents
            WHERE DocumentId = @DocumentId";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Document>(
            new CommandDefinition(sql, new { DocumentId = documentId }, cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT DocumentId, Title, FileName, BlobUrl, ContentType, FileSizeBytes,
                   Jurisdiction, TaxType, Version, EffectiveDate, UploadedBy,
                   CreatedAt, UpdatedAt, Status, ChunkCount, IndexedAt
            FROM Documents
            ORDER BY CreatedAt DESC";

        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Document>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
    }

    public async Task<Guid> CreateAsync(Document document, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO Documents (DocumentId, Title, FileName, BlobUrl, ContentType, FileSizeBytes,
                                   Jurisdiction, TaxType, Version, EffectiveDate, UploadedBy,
                                   CreatedAt, Status)
            VALUES (@DocumentId, @Title, @FileName, @BlobUrl, @ContentType, @FileSizeBytes,
                    @Jurisdiction, @TaxType, @Version, @EffectiveDate, @UploadedBy,
                    @CreatedAt, @Status)";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, document, cancellationToken: cancellationToken));

        _logger.LogInformation("Created document: {DocumentId}", document.DocumentId);
        return document.DocumentId;
    }

    public async Task UpdateAsync(Document document, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Documents
            SET Title = @Title,
                FileName = @FileName,
                BlobUrl = @BlobUrl,
                ContentType = @ContentType,
                FileSizeBytes = @FileSizeBytes,
                Jurisdiction = @Jurisdiction,
                TaxType = @TaxType,
                Version = @Version,
                EffectiveDate = @EffectiveDate,
                UpdatedAt = @UpdatedAt,
                Status = @Status,
                ChunkCount = @ChunkCount,
                IndexedAt = @IndexedAt
            WHERE DocumentId = @DocumentId";

        document.UpdatedAt = DateTimeOffset.UtcNow;

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, document, cancellationToken: cancellationToken));

        _logger.LogInformation("Updated document: {DocumentId}", document.DocumentId);
    }

    public async Task UpdateStatusAsync(Guid documentId, DocumentStatus status, int? chunkCount = null, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE Documents
            SET Status = @Status,
                ChunkCount = COALESCE(@ChunkCount, ChunkCount),
                IndexedAt = CASE WHEN @Status = 2 THEN SYSDATETIMEOFFSET() ELSE IndexedAt END,
                UpdatedAt = SYSDATETIMEOFFSET()
            WHERE DocumentId = @DocumentId";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { DocumentId = documentId, Status = (int)status, ChunkCount = chunkCount }, cancellationToken: cancellationToken));

        _logger.LogInformation("Updated document status: {DocumentId} to {Status}", documentId, status);
    }

    public async Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Documents WHERE DocumentId = @DocumentId";

        using var connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { DocumentId = documentId }, cancellationToken: cancellationToken));

        _logger.LogInformation("Deleted document: {DocumentId}", documentId);
    }
}

