namespace Masroof.Domain.Entities;

/// <summary>
/// A learned mapping of a normalized subject (merchant/person) to a category for one user.
/// Replaces kharcha's third-party memory. Embedded with bge-m3 for vector recall.
/// </summary>
public class MerchantRule
{
    public int RuleId { get; set; }
    public Guid UserId { get; set; }

    /// <summary>Normalized merchant/person, unique per user (see <c>UQ_Rule</c>).</summary>
    public string SubjectNorm { get; set; } = string.Empty;

    public short CategoryId { get; set; }

    /// <summary>Human-readable rule, injected as a hint, e.g. "Treat PANDA as groceries."</summary>
    public string RuleText { get; set; } = string.Empty;

    /// <summary>
    /// bge-m3 embedding (1024 dims), stored in SQL Server's native VECTOR column. This column
    /// is read/written by the rules engine via raw SQL (EF does not map the VECTOR type), so it
    /// is marked [NotMapped]; <see cref="EmbeddingModel"/> records whether/which model embedded it.
    /// </summary>
    public float[]? Embedding { get; set; }

    /// <summary>Name of the model that produced <see cref="Embedding"/>; null when not yet embedded.</summary>
    public string? EmbeddingModel { get; set; }

    public int HitCount { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? User { get; set; }
    public Category? Category { get; set; }
}
