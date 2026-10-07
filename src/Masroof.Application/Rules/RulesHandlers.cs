using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Rules;

/// <summary>A learned rule as shown on the "Learned rules" screen.</summary>
public sealed record RuleDto(
    int RuleId,
    string Subject,
    string CategoryCode,
    string CategoryName,
    string RuleText,
    int HitCount,
    bool HasEmbedding,
    DateTime UpdatedAt);

/// <summary>Lists what the system has learned for the current user, newest first.</summary>
public sealed class ListRulesHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<RuleDto>> HandleAsync(string? search, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var isArabic = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        IQueryable<MerchantRule> q = db.MerchantRules.Where(r => r.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            q = q.Where(r => EF.Functions.Like(r.SubjectNorm, $"%{term}%")
                             || EF.Functions.Like(r.RuleText, $"%{term}%"));
        }

        var rows = await q
            .OrderByDescending(r => r.UpdatedAt)
            .Select(r => new
            {
                r.RuleId,
                r.SubjectNorm,
                r.Category!.Code,
                r.Category.NameEn,
                r.Category.NameAr,
                r.RuleText,
                r.HitCount,
                HasEmbedding = r.EmbeddingModel != null,
                r.UpdatedAt
            })
            .ToListAsync(ct);

        return rows.Select(r => new RuleDto(
            r.RuleId, r.SubjectNorm, r.Code,
            isArabic ? r.NameAr : r.NameEn,
            r.RuleText, r.HitCount, r.HasEmbedding, r.UpdatedAt)).ToList();
    }
}

/// <summary>Forgets a learned rule (hard delete — the user explicitly asked to forget it).</summary>
public sealed class DeleteRuleHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task HandleAsync(int ruleId, CancellationToken ct)
    {
        var rule = await db.MerchantRules.FirstOrDefaultAsync(
                       r => r.RuleId == ruleId && r.UserId == currentUser.UserId, ct)
                   ?? throw new NotFoundException(nameof(MerchantRule), ruleId);

        db.MerchantRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }
}
