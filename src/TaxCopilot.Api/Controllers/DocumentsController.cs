using Microsoft.AspNetCore.Mvc;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Services;

namespace TaxCopilot.Api.Controllers;

/// <summary>
/// Controller for document operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DocumentService _documentService;
    private readonly IngestionService _ingestionService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        DocumentService documentService,
        IngestionService ingestionService,
        ILogger<DocumentsController> logger)
    {
        _documentService = documentService;
        _ingestionService = ingestionService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new document.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(DocumentUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string title,
        [FromForm] string jurisdiction,
        [FromForm] string taxType,
        [FromForm] string version,
        [FromForm] DateTimeOffset? effectiveDate,
        [FromForm] string uploadedBy,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        // Validate file type
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".pdf" && extension != ".docx")
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, "Unsupported file format. Only PDF and DOCX files are supported.");
        }

        _logger.LogInformation("Uploading document: {FileName}", file.FileName);

        using var stream = file.OpenReadStream();
        var result = await _documentService.UploadAsync(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            title,
            jurisdiction,
            taxType,
            version,
            effectiveDate,
            uploadedBy,
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { documentId = result.DocumentId }, result);
    }

    /// <summary>
    /// Get document by ID.
    /// </summary>
    [HttpGet("{documentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentService.GetByIdAsync(documentId, cancellationToken);
        if (document == null)
        {
            return NotFound();
        }
        return Ok(document);
    }

    /// <summary>
    /// Get all documents.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var documents = await _documentService.GetAllAsync(cancellationToken);
        return Ok(documents);
    }

    /// <summary>
    /// Ingest a document (extract, chunk, embed, and index).
    /// </summary>
    [HttpPost("{documentId:guid}/ingest")]
    [ProducesResponseType(typeof(IngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    public async Task<IActionResult> Ingest(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting ingestion for document: {DocumentId}", documentId);

            var result = await _ingestionService.IngestDocumentAsync(documentId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(ex.Message);
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status415UnsupportedMediaType, ex.Message);
        }
    }
}

