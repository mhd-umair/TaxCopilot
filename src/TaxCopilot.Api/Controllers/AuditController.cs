using Microsoft.AspNetCore.Mvc;
using TaxCopilot.Application.Interfaces;

namespace TaxCopilot.Api.Controllers;

/// <summary>
/// Controller for audit log operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditController> _logger;

    public AuditController(IAuditLogRepository auditLogRepository, ILogger<AuditController> logger)
    {
        _auditLogRepository = auditLogRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get recent audit logs.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving {Count} audit logs", take);

        var logs = await _auditLogRepository.GetRecentAsync(take, cancellationToken);
        return Ok(logs);
    }
}

