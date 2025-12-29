using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Services;

/// <summary>
/// Service for document ingestion pipeline.
/// </summary>
public class IngestionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ITextExtractor _textExtractor;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISearchService _searchService;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IDocumentRepository documentRepository,
        IBlobStorageService blobStorage,
        ITextExtractor textExtractor,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ISearchService searchService,
        ILogger<IngestionService> logger)
    {
        _documentRepository = documentRepository;
        _blobStorage = blobStorage;
        _textExtractor = textExtractor;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _searchService = searchService;
        _logger = logger;
    }

    public async Task<IngestResponse> IngestDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting ingestion for document: {DocumentId}", documentId);

        // Load document metadata
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            throw new InvalidOperationException($"Document not found: {documentId}");
        }

        // Check file type support
        if (!_textExtractor.SupportsFileType(document.FileName))
        {
            throw new NotSupportedException($"Unsupported file format: {Path.GetExtension(document.FileName)}");
        }

        // Update status to processing
        await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: cancellationToken);

        try
        {
            // Download blob
            _logger.LogInformation("Downloading blob for document: {DocumentId}", documentId);
            using var blobStream = await _blobStorage.DownloadAsync(document.BlobUrl, cancellationToken);

            // Extract text
            _logger.LogInformation("Extracting text from document: {DocumentId}", documentId);
            var pages = await _textExtractor.ExtractAsync(blobStream, document.FileName, cancellationToken);

            if (pages.Count == 0)
            {
                _logger.LogWarning("No text extracted from document: {DocumentId}", documentId);
            }

            // Chunk the text
            _logger.LogInformation("Chunking document: {DocumentId}", documentId);
            var chunks = _chunkingService.ChunkDocument(
                pages,
                documentId,
                document.Title,
                document.Jurisdiction,
                document.TaxType,
                document.Version,
                document.EffectiveDate);

            _logger.LogInformation("Created {ChunkCount} chunks for document: {DocumentId}", chunks.Count, documentId);

            // Generate embeddings for each chunk
            _logger.LogInformation("Generating embeddings for {ChunkCount} chunks", chunks.Count);
            var texts = chunks.Select(c => c.ChunkText).ToList();
            var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts, cancellationToken);

            for (int i = 0; i < chunks.Count; i++)
            {
                chunks[i].Embedding = embeddings[i];
            }

            // Delete existing chunks for this document (re-ingestion case)
            await _searchService.DeleteDocumentChunksAsync(documentId, cancellationToken);

            // Index chunks
            _logger.LogInformation("Indexing {ChunkCount} chunks for document: {DocumentId}", chunks.Count, documentId);
            var indexedCount = await _searchService.IndexChunksAsync(chunks, cancellationToken);

            // Update document status
            await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Indexed, chunks.Count, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Ingestion completed for document: {DocumentId} in {ElapsedMs}ms", documentId, stopwatch.ElapsedMilliseconds);

            return new IngestResponse
            {
                DocumentId = documentId,
                ChunksCreated = chunks.Count,
                ChunksIndexed = indexedCount,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion failed for document: {DocumentId}", documentId);
            await _documentRepository.UpdateStatusAsync(documentId, DocumentStatus.Failed, cancellationToken: cancellationToken);
            throw;
        }
    }
}

