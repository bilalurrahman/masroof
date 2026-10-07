using Dapper;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Normalization;
using Masroof.Infrastructure.Llm;
using Masroof.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Masroof.Infrastructure.Rules;

/// <summary>
/// Runs the four-stage rules engine: exact and fuzzy (Jaro-Winkler ≥ 0.92) on the
/// counterparty, then vector recall (bge-m3 + cosine) with a lexical gate that drops hits
/// unless ≥ 50% of the rule's subject tokens appear in the message. Owns the VECTOR column I/O.
/// </summary>
public sealed class RuleStore(
    MasroofDbContext db,
    ISqlConnectionFactory sql,
    IEmbeddingService embeddings,
    IOptions<LlmOptions> options,
    ILogger<RuleStore> logger)
    : IRuleStore
{
    private const double FuzzyThreshold = 0.92;
    private const double LexicalGateRatio = 0.5;
    private const int VectorTopK = 5;

    private readonly int _dimensions = options.Value.EmbeddingDimensions;

    public async Task<IReadOnlyList<RuleMatch>> MatchAsync(
        Guid userId, string normalizedMessage, string? counterpartyNorm, CancellationToken ct)
    {
        var rules = await db.MerchantRules
            .IgnoreQueryFilters()
            .Where(r => r.UserId == userId)
            .Select(r => new RuleRow(r.RuleId, r.SubjectNorm, r.CategoryId, r.Category!.Code, r.RuleText))
            .ToListAsync(ct);

        if (rules.Count == 0)
            return [];

        var best = new Dictionary<int, RuleMatch>();

        // --- Exact + fuzzy on the counterparty ---
        if (!string.IsNullOrWhiteSpace(counterpartyNorm))
        {
            foreach (var r in rules)
            {
                if (string.Equals(r.SubjectNorm, counterpartyNorm, StringComparison.OrdinalIgnoreCase))
                {
                    Consider(best, r, RuleMatchTier.Exact, 1.0);
                    continue;
                }
                var sim = JaroWinkler.Similarity(r.SubjectNorm, counterpartyNorm);
                if (sim >= FuzzyThreshold)
                    Consider(best, r, RuleMatchTier.Fuzzy, sim);
            }
        }

        // --- Vector recall + lexical gate (best effort; skip if embeddings are down) ---
        try
        {
            var messageTokens = new HashSet<string>(
                TextNormalizer.Tokenize(TextNormalizer.NormalizeSubject(normalizedMessage)),
                StringComparer.OrdinalIgnoreCase);

            var queryVector = await embeddings.EmbedAsync(normalizedMessage, ct);
            var hits = await VectorSearchAsync(userId, queryVector, ct);

            var byId = rules.ToDictionary(r => r.RuleId);
            foreach (var (ruleId, distance) in hits)
            {
                if (!byId.TryGetValue(ruleId, out var r))
                    continue;
                if (!PassesLexicalGate(r.SubjectNorm, messageTokens))
                    continue;
                Consider(best, r, RuleMatchTier.Vector, 1.0 - distance);
            }
        }
        catch (LlmUnavailableException ex)
        {
            logger.LogDebug(ex, "Vector rule tier skipped (embeddings unavailable).");
        }

        return best.Values
            .OrderBy(m => m.Tier)
            .ThenByDescending(m => m.Score)
            .ToList();
    }

    public async Task SetEmbeddingAsync(int ruleId, float[] embedding, CancellationToken ct)
    {
        await using var conn = await sql.OpenAsync(ct);
        var literal = VectorLiteral.From(embedding);
        var cmd = new CommandDefinition(
            $"UPDATE dbo.MerchantRules SET Embedding = CAST(@q AS VECTOR({_dimensions})), EmbeddingModel = @model WHERE RuleId = @ruleId",
            new { q = literal, model = options.Value.EmbedModel, ruleId },
            cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    public async Task IncrementHitCountAsync(int ruleId, CancellationToken ct)
    {
        await using var conn = await sql.OpenAsync(ct);
        var cmd = new CommandDefinition(
            "UPDATE dbo.MerchantRules SET HitCount = HitCount + 1 WHERE RuleId = @ruleId",
            new { ruleId },
            cancellationToken: ct);
        await conn.ExecuteAsync(cmd);
    }

    private async Task<IReadOnlyList<(int RuleId, double Distance)>> VectorSearchAsync(
        Guid userId, float[] queryVector, CancellationToken ct)
    {
        await using var conn = await sql.OpenAsync(ct);
        var sqlText =
            $"""
             SELECT TOP (@k) RuleId,
                    VECTOR_DISTANCE('cosine', Embedding, CAST(@q AS VECTOR({_dimensions}))) AS Dist
             FROM dbo.MerchantRules
             WHERE UserId = @userId AND Embedding IS NOT NULL
             ORDER BY Dist;
             """;
        var cmd = new CommandDefinition(sqlText,
            new { k = VectorTopK, q = VectorLiteral.From(queryVector), userId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<(int RuleId, double Dist)>(cmd);
        return rows.Select(r => (r.RuleId, r.Dist)).ToList();
    }

    private static bool PassesLexicalGate(string subjectNorm, HashSet<string> messageTokens)
    {
        var subjectTokens = TextNormalizer.Tokenize(subjectNorm);
        if (subjectTokens.Length == 0)
            return false;
        var present = subjectTokens.Count(messageTokens.Contains);
        return present / (double)subjectTokens.Length >= LexicalGateRatio;
    }

    private static void Consider(IDictionary<int, RuleMatch> best, RuleRow r, RuleMatchTier tier, double score)
    {
        var candidate = new RuleMatch(r.RuleId, r.SubjectNorm, r.CategoryId, r.CategoryCode, r.RuleText, tier, score);
        if (!best.TryGetValue(r.RuleId, out var existing)
            || tier < existing.Tier
            || (tier == existing.Tier && score > existing.Score))
        {
            best[r.RuleId] = candidate;
        }
    }

    private sealed record RuleRow(int RuleId, string SubjectNorm, short CategoryId, string CategoryCode, string RuleText);
}
