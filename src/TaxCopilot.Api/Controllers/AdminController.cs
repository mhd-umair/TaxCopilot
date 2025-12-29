using Microsoft.AspNetCore.Mvc;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Interfaces;

namespace TaxCopilot.Api.Controllers;

/// <summary>
/// Controller for admin operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly IBlobStorageService _blobStorage;
    private readonly ISearchService _searchService;
    private readonly ISqlConnectionFactory _sqlConnectionFactory;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatService _chatService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IBlobStorageService blobStorage,
        ISearchService searchService,
        ISqlConnectionFactory sqlConnectionFactory,
        IEmbeddingService embeddingService,
        IChatService chatService,
        ILogger<AdminController> logger)
    {
        _blobStorage = blobStorage;
        _searchService = searchService;
        _sqlConnectionFactory = sqlConnectionFactory;
        _embeddingService = embeddingService;
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Initialize required resources (blob container, search index).
    /// </summary>
    [HttpPost("init")]
    [ProducesResponseType(typeof(InitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Initialize(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing resources");

        var response = new InitResponse();

        try
        {
            // Ensure blob container exists
            response.BlobContainerCreated = await _blobStorage.EnsureContainerExistsAsync(cancellationToken);

            // Ensure search index exists
            response.SearchIndexCreated = await _searchService.EnsureIndexExistsAsync(cancellationToken);

            response.Success = true;
            response.Message = "Resources initialized successfully";

            _logger.LogInformation("Resources initialized: BlobContainerCreated={BlobCreated}, SearchIndexCreated={SearchCreated}",
                response.BlobContainerCreated, response.SearchIndexCreated);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize resources");
            response.Success = false;
            response.Message = ex.Message;
            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthCheckResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken)
    {
        var response = new HealthCheckResponse();
        var allHealthy = true;

        // Check SQL connection
        try
        {
            var sqlHealthy = await _sqlConnectionFactory.TestConnectionAsync(cancellationToken);
            response.Components["SqlDatabase"] = new ComponentHealth
            {
                Healthy = sqlHealthy,
                Message = sqlHealthy ? "Connected" : "Connection failed"
            };
            allHealthy &= sqlHealthy;
        }
        catch (Exception ex)
        {
            response.Components["SqlDatabase"] = new ComponentHealth
            {
                Healthy = false,
                Message = ex.Message
            };
            allHealthy = false;
        }

        // Check blob container
        try
        {
            var blobHealthy = await _blobStorage.ContainerExistsAsync(cancellationToken);
            response.Components["BlobStorage"] = new ComponentHealth
            {
                Healthy = blobHealthy,
                Message = blobHealthy ? "Container exists" : "Container not found"
            };
            allHealthy &= blobHealthy;
        }
        catch (Exception ex)
        {
            response.Components["BlobStorage"] = new ComponentHealth
            {
                Healthy = false,
                Message = ex.Message
            };
            allHealthy = false;
        }

        // Check search index
        try
        {
            var searchHealthy = await _searchService.IndexExistsAsync(cancellationToken);
            response.Components["AzureSearch"] = new ComponentHealth
            {
                Healthy = searchHealthy,
                Message = searchHealthy ? "Index exists" : "Index not found"
            };
            allHealthy &= searchHealthy;
        }
        catch (Exception ex)
        {
            response.Components["AzureSearch"] = new ComponentHealth
            {
                Healthy = false,
                Message = ex.Message
            };
            allHealthy = false;
        }

        // Check Azure OpenAI embeddings
        try
        {
            var embeddingHealthy = await _embeddingService.IsAvailableAsync(cancellationToken);
            response.Components["AzureOpenAI_Embeddings"] = new ComponentHealth
            {
                Healthy = embeddingHealthy,
                Message = embeddingHealthy ? "Available" : "Not available"
            };
            allHealthy &= embeddingHealthy;
        }
        catch (Exception ex)
        {
            response.Components["AzureOpenAI_Embeddings"] = new ComponentHealth
            {
                Healthy = false,
                Message = ex.Message
            };
            allHealthy = false;
        }

        // Check Azure OpenAI chat
        try
        {
            var chatHealthy = await _chatService.IsAvailableAsync(cancellationToken);
            response.Components["AzureOpenAI_Chat"] = new ComponentHealth
            {
                Healthy = chatHealthy,
                Message = chatHealthy ? "Available" : "Not available"
            };
            allHealthy &= chatHealthy;
        }
        catch (Exception ex)
        {
            response.Components["AzureOpenAI_Chat"] = new ComponentHealth
            {
                Healthy = false,
                Message = ex.Message
            };
            allHealthy = false;
        }

        response.Status = allHealthy ? "Healthy" : "Unhealthy";

        return allHealthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}

