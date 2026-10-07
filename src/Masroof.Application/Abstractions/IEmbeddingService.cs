namespace Masroof.Application.Abstractions;

/// <summary>Produces multilingual embeddings (bge-m3, 1024 dims) for rule text and messages.</summary>
public interface IEmbeddingService
{
    /// <summary>Embedding dimensionality (1024 for bge-m3).</summary>
    int Dimensions { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
