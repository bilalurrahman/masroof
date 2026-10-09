namespace Masroof.Application.Abstractions;

/// <summary>A category suggestion from a dedicated classifier: a taxonomy code and its confidence.</summary>
public sealed record CategorySuggestion(string Code, double Confidence);

/// <summary>
/// An optional dedicated text classifier for the category step (e.g. an external zero-shot
/// classification model). When <see cref="IsEnabled"/> is false the parse pipeline relies on
/// the local LLM instead. Implementations must never throw from <see cref="ClassifyAsync"/> —
/// they return null on any failure so the caller can fall back.
/// </summary>
public interface ICategoryClassifier
{
    /// <summary>True when a classifier is configured and ready. Gated by config so the
    /// zero-egress default (no external calls) is preserved unless explicitly enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Classifies the message into a taxonomy code, or null if unavailable/unsure.</summary>
    Task<CategorySuggestion?> ClassifyAsync(string text, CancellationToken ct);
}
