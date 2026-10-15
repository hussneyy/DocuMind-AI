using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DocuMind.Application.Embeddings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocuMind.Infrastructure.Embeddings;

/// <summary>
/// Calls OpenAI's (or an Azure OpenAI / OpenAI-compatible) embeddings endpoint. Never invoked
/// unless <c>Embedding:Provider</c> is set to "OpenAI" — see <see cref="MockEmbeddingProvider"/>
/// for the default, no-API-key development path.
/// </summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly HttpClient _httpClient;
    private readonly EmbeddingOptions _options;
    private readonly ILogger<OpenAiEmbeddingProvider> _logger;

    public OpenAiEmbeddingProvider(HttpClient httpClient, IOptions<EmbeddingOptions> options, ILogger<OpenAiEmbeddingProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(1, _options.TimeoutSeconds));
        _httpClient.BaseAddress = new Uri(
            string.IsNullOrWhiteSpace(_options.Endpoint) ? "https://api.openai.com/v1/" : _options.Endpoint);
    }

    public string ModelId => _options.Model;

    public int Dimensions => _options.Dimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var results = await EmbedBatchAsync([text], ct);
        return results[0];
    }

    public async Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Embedding:Provider is set to 'OpenAI' but Embedding:ApiKey is not configured. " +
                "Set EMBEDDING_PROVIDER=Mock to run without a paid API key.");
        }

        var maxAttempts = Math.Max(1, _options.MaxRetries) + 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "embeddings");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
                request.Content = JsonContent.Create(new { model = _options.Model, input = texts });

                response = await _httpClient.SendAsync(request, ct);
            }
            catch (Exception ex) when (attempt < maxAttempts && ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Embedding request failed (attempt {Attempt}/{MaxAttempts}); retrying", attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), ct);
                continue;
            }

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var payload = await response.Content.ReadFromJsonAsync<OpenAiEmbeddingResponse>(jsonOptions, ct)
                    ?? throw new InvalidOperationException("Embedding provider returned an empty response.");

                _logger.LogInformation("Generated {Count} embeddings via {Model}", texts.Count, _options.Model);

                return payload.Data.OrderBy(d => d.Index).Select(d => d.Embedding).ToList();
            }

            var isTransient = response.StatusCode is System.Net.HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500;
            if (isTransient && attempt < maxAttempts)
            {
                _logger.LogWarning("Embedding request returned {StatusCode} (attempt {Attempt}/{MaxAttempts}); retrying",
                    response.StatusCode, attempt, maxAttempts);
                await Task.Delay(TimeSpan.FromMilliseconds(300 * attempt), ct);
                continue;
            }

            _logger.LogError("Embedding request failed with status {StatusCode}", response.StatusCode);
            throw new InvalidOperationException($"Embedding provider request failed with status {(int)response.StatusCode}.");
        }

        throw new InvalidOperationException("Embedding provider request failed after all retry attempts.");
    }

    private record OpenAiEmbeddingResponse([property: JsonPropertyName("data")] List<OpenAiEmbeddingData> Data);

    private record OpenAiEmbeddingData(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
