using System.Data;

namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Factory for creating SQL connections.
/// </summary>
public interface ISqlConnectionFactory
{
    IDbConnection CreateConnection();
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

