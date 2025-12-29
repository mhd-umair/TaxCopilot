using Microsoft.AspNetCore.Mvc;
using TaxCopilot.Api.Middleware;
using TaxCopilot.Application.DTOs;
using TaxCopilot.Application.Services;

namespace TaxCopilot.Api.Controllers;

/// <summary>
/// Controller for RAG chat operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly RagService _ragService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(RagService ragService, ILogger<ChatController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    /// <summary>
    /// Ask a question using RAG.
    /// </summary>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest("Question is required");
        }

        var correlationId = HttpContext.GetCorrelationId();

        _logger.LogInformation("Processing question: {Question}", request.Question);

        var result = await _ragService.AskAsync(
            request.Question,
            request.Filters,
            correlationId,
            cancellationToken);

        return Ok(result);
    }
}

