using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaxCopilot.Application.Configuration;
using TaxCopilot.Application.Interfaces;

namespace TaxCopilot.Infrastructure.Azure;

/// <summary>
/// Azure Blob Storage service implementation.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _containerName;

    public BlobStorageService(IOptions<AzureBlobOptions> options, ILogger<BlobStorageService> logger)
    {
        var blobOptions = options.Value;
        _containerName = blobOptions.ContainerName;
        _logger = logger;

        var blobServiceClient = new BlobServiceClient(blobOptions.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
    }

    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        // Generate unique blob name
        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        _logger.LogInformation("Uploading blob: {BlobName}", blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        _logger.LogInformation("Blob uploaded successfully: {BlobUri}", blobClient.Uri);

        return blobClient.Uri.ToString();
    }

    public async Task<Stream> DownloadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(blobUrl);
        var blobName = GetBlobNameFromUri(uri);
        var blobClient = _containerClient.GetBlobClient(blobName);

        _logger.LogInformation("Downloading blob: {BlobName}", blobName);

        var response = await blobClient.DownloadAsync(cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var uri = new Uri(blobUrl);
        var blobName = GetBlobNameFromUri(uri);
        var blobClient = _containerClient.GetBlobClient(blobName);

        _logger.LogInformation("Deleting blob: {BlobName}", blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<bool> EnsureContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            if (response != null)
            {
                _logger.LogInformation("Created blob container: {ContainerName}", _containerName);
                return true;
            }
            _logger.LogInformation("Blob container already exists: {ContainerName}", _containerName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure blob container exists: {ContainerName}", _containerName);
            throw;
        }
    }

    public async Task<bool> ContainerExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _containerClient.ExistsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check blob container existence");
            return false;
        }
    }

    private static string GetBlobNameFromUri(Uri uri)
    {
        // Extract blob name from URI path (skip container name)
        var path = uri.AbsolutePath;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        // First segment is container name, rest is blob name
        if (segments.Length > 1)
        {
            return string.Join("/", segments.Skip(1));
        }

        throw new ArgumentException($"Invalid blob URL: {uri}");
    }
}

