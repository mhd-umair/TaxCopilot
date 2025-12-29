using Microsoft.Extensions.Logging;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;
using TaxCopilot.Domain.Entities;

namespace TaxCopilot.Application.Services;

/// <summary>
/// Service for document operations.
/// </summary>
public class DocumentService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IBlobStorageService blobStorage,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    public async Task<DocumentUploadResponse> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileSize,
        string title,
        string jurisdiction,
        string taxType,
        string version,
        DateTimeOffset? effectiveDate,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document: {FileName}", fileName);

        // Upload to blob storage
        var blobUrl = await _blobStorage.UploadAsync(fileStream, fileName, contentType, cancellationToken);

        // Create document record
        var document = new Document
        {
            DocumentId = Guid.NewGuid(),
            Title = title,
            FileName = fileName,
            BlobUrl = blobUrl,
            ContentType = contentType,
            FileSizeBytes = fileSize,
            Jurisdiction = jurisdiction,
            TaxType = taxType,
            Version = version,
            EffectiveDate = effectiveDate,
            UploadedBy = uploadedBy,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = DocumentStatus.Uploaded
        };

        await _documentRepository.CreateAsync(document, cancellationToken);

        _logger.LogInformation("Document uploaded successfully: {DocumentId}", document.DocumentId);

        return new DocumentUploadResponse
        {
            DocumentId = document.DocumentId,
            BlobUrl = blobUrl
        };
    }

    public async Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return await _documentRepository.GetByIdAsync(documentId, cancellationToken);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _documentRepository.GetAllAsync(cancellationToken);
    }
}

