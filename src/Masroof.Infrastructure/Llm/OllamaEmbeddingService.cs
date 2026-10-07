using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Microsoft.Extensions.Options;

namespace Masroof.Infrastructure.Llm;

/// <summary>Produces bge-m3 embeddings via Ollama's <c>/api/embed</c> endpoint.</summary>
public sealed class OllamaEmbeddingService(HttpClient http, IOptions<LlmOptions> options) : IEmbeddingService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly LlmOptions _options = options.Value;

    public int Dimensions => _options.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var request = new { model = _options.EmbedModel, input = text };
        try
        {
            using var response = await http.PostAsJsonAsync("/api/embed", request, Json, ct);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadFromJsonAsync<EmbedResponse>(Json, ct);
            var vector = body?.Embeddings?.FirstOrDefault();
            if (vector is null || vector.Length == 0)
                throw new LlmUnavailableException("Embedding model returned no vector.");
            return vector;
        }
        catch (HttpRequestException ex)
        {
            throw new LlmUnavailableException("The embedding runtime is unreachable.", ex);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new LlmUnavailableException("The embedding request timed out.");
        }
    }

    private sealed class EmbedResponse
    {
        [JsonPropertyName("embeddings")] public float[][]? Embeddings { get; set; }
    }
}
