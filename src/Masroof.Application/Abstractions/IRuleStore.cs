namespace Masroof.Application.Abstractions;

/// <summary>Which matching tier produced a rule hit. Lower = stronger evidence.</summary>
public enum RuleMatchTier
{
    Exact = 0,
    Fuzzy = 1,
    Vector = 2
}

/// <summary>A rule that matched the incoming message, with the tier and score that found it.</summary>
public sealed record RuleMatch(
    int RuleId,
    string SubjectNorm,
    short CategoryId,
    string CategoryCode,
    string RuleText,
    RuleMatchTier Tier,
    double Score);

/// <summary>
/// The rules engine (replacing Backboard). Runs exact → fuzzy → vector matching with a
/// lexical gate, and owns the SQL Server VECTOR column reads/writes that EF cannot express.
/// </summary>
public interface IRuleStore
{
    /// <summary>
    /// Returns rule hints for a message, best first. An exact hit (if any) is first and is
    /// authoritative — the caller forces that category after the LLM responds.
    /// </summary>
    Task<IReadOnlyList<RuleMatch>> MatchAsync(
        Guid userId,
        string normalizedMessage,
        string? counterpartyNorm,
        CancellationToken ct);

    /// <summary>Writes the bge-m3 embedding into the rule's VECTOR column.</summary>
    Task SetEmbeddingAsync(int ruleId, float[] embedding, CancellationToken ct);

    /// <summary>Bumps HitCount when a rule actually drove a categorization.</summary>
    Task IncrementHitCountAsync(int ruleId, CancellationToken ct);
}
