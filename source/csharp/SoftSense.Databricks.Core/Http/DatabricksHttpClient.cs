using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using SoftSense.Databricks.Core.Configuration;
using SoftSense.Databricks.Core.Exceptions;

namespace SoftSense.Databricks.Core.Http;

/// <summary>
/// HTTP client for making requests to Databricks API
/// </summary>
public sealed class DatabricksHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly DatabricksConfig _config;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly TokenRequestContext? _tokenContext;

    public DatabricksHttpClient(DatabricksConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _config.Validate();

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_config.WorkspaceUrl),
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };

        // Set up token context for Azure Entra authentication
        if (_config.Credential is not null)
        {
            _tokenContext = new TokenRequestContext(
                new[] { $"https://{_config.AzureResourceId}/.default" });
        }
        else if (!string.IsNullOrWhiteSpace(_config.AccessToken))
        {
            // PAT authentication
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.AccessToken);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("SoftSense.Databricks.Client", "0.1.0"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Gets a fresh access token from Azure Entra ID
    /// </summary>
    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_config.Credential is null || _tokenContext is null)
        {
            throw new DatabricksAuthenticationException(
                "Azure Entra credential not configured");
        }

        try
        {
            var token = await _config.Credential.GetTokenAsync(_tokenContext.Value, cancellationToken);
            return token.Token;
        }
        catch (Exception ex)
        {
            throw new DatabricksAuthenticationException(
                "Failed to acquire token from Azure Entra ID", ex);
        }
    }

    /// <summary>
    /// Sends a GET request
    /// </summary>
    public async Task<TResponse> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<TResponse>(
            HttpMethod.Get,
            endpoint,
            null,
            cancellationToken);
    }

    /// <summary>
    /// Sends a POST request
    /// </summary>
    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<TResponse>(
            HttpMethod.Post,
            endpoint,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Sends a GET request and returns a stream for reading response as it arrives
    /// </summary>
    public async Task<Stream> GetStreamAsync(
        string endpoint,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestInternalAsync(
            HttpMethod.Get,
            endpoint,
            null,
            cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Sends a POST request and returns a stream for reading response as it arrives
    /// </summary>
    public async Task<Stream> PostStreamAsync<TRequest>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await SendRequestInternalAsync(
            HttpMethod.Post,
            endpoint,
            request,
            cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Sends an HTTP request with retry logic
    /// </summary>
    private async Task<TResponse> SendRequestAsync<TResponse>(
        HttpMethod method,
        string endpoint,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        var response = await SendRequestInternalAsync(method, endpoint, requestBody, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var result = JsonSerializer.Deserialize<TResponse>(content, _jsonOptions);
            return result ?? throw new DatabricksException("Failed to deserialize response");
        }
        catch (JsonException ex)
        {
            throw new DatabricksException($"Failed to parse response: {content}", ex);
        }
    }

    /// <summary>
    /// Core request sending with retry logic
    /// </summary>
    private async Task<HttpResponseMessage> SendRequestInternalAsync(
        HttpMethod method,
        string endpoint,
        object? requestBody,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= _config.MaxRetries)
        {
            try
            {
                using var request = new HttpRequestMessage(method, endpoint);

                // Set authorization header with fresh token if using Azure Entra
                if (_config.Credential is not null)
                {
                    var token = await GetAccessTokenAsync(cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                }

                if (requestBody is not null)
                {
                    request.Content = JsonContent.Create(requestBody, options: _jsonOptions);
                }

                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // Handle specific error cases
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new DatabricksAuthenticationException(
                        $"Authentication failed: {response.StatusCode} - {content}");
                }

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new DatabricksRateLimitException(
                        (int)response.StatusCode,
                        $"Rate limit exceeded: {content}",
                        retryAfter);
                }

                // Retry on 5xx errors
                if ((int)response.StatusCode >= 500 && attempt < _config.MaxRetries)
                {
                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }

                // Throw for other error codes
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new DatabricksHttpException(
                    (int)response.StatusCode,
                    $"Request failed: {response.StatusCode} - {errorContent}",
                    errorContent);
            }
            catch (DatabricksException)
            {
                throw; // Re-throw Databricks exceptions
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                if (attempt < _config.MaxRetries)
                {
                    attempt++;
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    await Task.Delay(delay, cancellationToken);
                    continue;
                }
            }

            break;
        }

        throw new DatabricksException(
            $"Request failed after {attempt} attempts",
            lastException ?? new Exception("Unknown error"));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
