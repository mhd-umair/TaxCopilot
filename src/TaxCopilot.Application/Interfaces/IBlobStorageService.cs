namespace TaxCopilot.Application.Interfaces;

/// <summary>
/// Interface for blob storage operations.
/// </summary>
public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default);
    Task<bool> EnsureContainerExistsAsync(CancellationToken cancellationToken = default);
    Task<bool> ContainerExistsAsync(CancellationToken cancellationToken = default);
}

