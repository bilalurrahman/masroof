using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Masroof.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Transactions.GetLedger;

/// <summary>Returns a filtered, paged ledger scoped to the current user (excludes soft-deleted rows).</summary>
public sealed class GetLedgerHandler(IAppDbContext db, ICurrentUser currentUser)
{
    private const int MaxPageSize = 200;
    private const decimal NeedsReviewThreshold = 0.7m;

    public async Task<PagedResult<TransactionDto>> HandleAsync(GetLedgerQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var page = Math.Max(query.Page, 1);
        var isArabic = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

        IQueryable<Transaction> q = db.Transactions.Where(t => t.UserId == userId && !t.IsDeleted);

        if (query is { Year: { } y, Month: { } m })
        {
            var from = new DateOnly(y, m, 1);
            var to = from.AddMonths(1);
            q = q.Where(t => t.TxnDate >= from && t.TxnDate < to);
        }

        if (!string.IsNullOrWhiteSpace(query.CategoryCode))
            q = q.Where(t => t.Category!.Code == query.CategoryCode);

        if (query.Direction is "debit" or "credit")
        {
            var dir = Domain.Enums.TransactionDirectionExtensions.ParseDirection(query.Direction);
            q = q.Where(t => t.Direction == dir);
        }

        if (query.AccountId is { } accountId)
            q = q.Where(t => t.AccountId == accountId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t => (t.Counterparty != null && EF.Functions.Like(t.Counterparty, $"%{term}%"))
                             || EF.Functions.Like(t.RawText, $"%{term}%"));
        }

        if (query.NeedsReview)
            q = q.Where(t => t.Confidence == null || t.Confidence < NeedsReviewThreshold);

        var total = await q.LongCountAsync(ct);

        var rows = await q
            .OrderByDescending(t => t.TxnDate)
            .ThenByDescending(t => t.TransactionId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.TransactionId,
                t.Direction,
                t.Amount,
                t.Currency,
                t.Counterparty,
                t.Channel,
                t.TxnDate,
                t.Category!.Code,
                NameEn = t.Category.NameEn,
                NameAr = t.Category.NameAr,
                t.Confidence,
                t.Source,
                t.IsCorrected,
                t.CreatedAt
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new TransactionDto(
            r.TransactionId,
            r.Direction.ToDbValue(),
            r.Amount,
            r.Currency,
            r.Counterparty,
            r.Channel,
            r.TxnDate,
            new CategoryRef(r.Code, isArabic ? r.NameAr : r.NameEn),
            r.Confidence,
            r.Source.ToDbValue(),
            r.IsCorrected,
            r.CreatedAt)).ToList();

        return new PagedResult<TransactionDto>(items, page, pageSize, total);
    }
}
