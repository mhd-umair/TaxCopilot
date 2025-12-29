using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using TaxCopilot.Contracts.DTOs;

namespace TaxCopilot.Ui.Services;

/// <summary>
/// Typed HTTP client for TaxCopilot API.
/// </summary>
public class TaxCopilotApiClient
{
    private readonly HttpClient _httpClient;
    private readonly CorrelationIdService _correlationIdService;
    private readonly ILogger<TaxCopilotApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TaxCopilotApiClient(
        HttpClient httpClient,
        CorrelationIdService correlationIdService,
        ILogger<TaxCopilotApiClient> logger)
    {
        _httpClient = httpClient;
        _correlationIdService = correlationIdService;
        _logger = logger;
    }

    private void SetCorrelationId(HttpRequestMessage request)
    {
        var correlationId = _correlationIdService.GenerateNew();
        request.Headers.Add("X-Correlation-Id", correlationId);
    }

    public async Task<ApiResult<InitResponse>> InitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/admin/init");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<InitResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize services");
            return ApiResult<InitResponse>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<HealthCheckResponse>> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/admin/health");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<HealthCheckResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get health status");
            return ApiResult<HealthCheckResponse>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<DocumentUploadResponse>> UploadAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string title,
        string jurisdiction,
        string taxType,
        string version,
        DateTimeOffset? effectiveDate,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(title), "title");
            content.Add(new StringContent(jurisdiction), "jurisdiction");
            content.Add(new StringContent(taxType), "taxType");
            content.Add(new StringContent(version), "version");
            if (effectiveDate.HasValue)
            {
                content.Add(new StringContent(effectiveDate.Value.ToString("o")), "effectiveDate");
            }
            content.Add(new StringContent(uploadedBy), "uploadedBy");

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/documents/upload");
            SetCorrelationId(request);
            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<DocumentUploadResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload document");
            return ApiResult<DocumentUploadResponse>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<List<DocumentDto>>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/documents");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<List<DocumentDto>>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents");
            return ApiResult<List<DocumentDto>>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<DocumentDto>> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/documents/{documentId}");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<DocumentDto>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document {DocumentId}", documentId);
            return ApiResult<DocumentDto>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<IngestResponse>> IngestAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"api/documents/{documentId}/ingest");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<IngestResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest document {DocumentId}", documentId);
            return ApiResult<IngestResponse>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<AskResponse>> AskAsync(
        string question,
        QueryFilters? filters,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var askRequest = new AskRequest
            {
                Question = question,
                Filters = filters
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/chat/ask");
            SetCorrelationId(request);
            request.Content = JsonContent.Create(askRequest);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<AskResponse>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ask question");
            return ApiResult<AskResponse>.Failure(ex.Message);
        }
    }

    public async Task<ApiResult<List<AuditLogDto>>> GetAuditLogsAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/audit?take={take}");
            SetCorrelationId(request);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            return await HandleResponseAsync<List<AuditLogDto>>(response, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get audit logs");
            return ApiResult<List<AuditLogDto>>.Failure(ex.Message);
        }
    }

    private async Task<ApiResult<T>> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
            return ApiResult<T>.Success(data!);
        }

        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("API request failed with status {StatusCode}: {Error}", response.StatusCode, errorContent);
        return ApiResult<T>.Failure($"API error ({(int)response.StatusCode}): {errorContent}");
    }
}

/// <summary>
/// Result wrapper for API calls.
/// </summary>
public class ApiResult<T>
{
    public bool IsSuccess { get; private set; }
    public T? Data { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ApiResult<T> Success(T data) => new() { IsSuccess = true, Data = data };
    public static ApiResult<T> Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

