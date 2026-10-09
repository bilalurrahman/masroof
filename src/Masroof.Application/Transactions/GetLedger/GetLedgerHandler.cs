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

        // Collapse paired internal transfers to a single row: keep the debit (primary) leg and
        // hide the credit (secondary) leg. Unpaired rows are unaffected.
        q = q.Where(t => t.TransferGroupId == null || t.Direction == TransactionDirection.Debit);

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
                t.CreatedAt,
                t.AccountId,
                t.TransferGroupId
            })
            .ToListAsync(ct);

        // For paired internal transfers, resolve "from → to" account labels for the row.
        var groupIds = rows.Where(r => r.TransferGroupId != null)
            .Select(r => r.TransferGroupId!.Value).Distinct().ToList();
        var destAccountByGroup = new Dictionary<Guid, int?>();
        var labelByAccount = new Dictionary<int, string>();
        if (groupIds.Count > 0)
        {
            destAccountByGroup = (await db.Transactions
                    .Where(t => t.TransferGroupId != null && groupIds.Contains(t.TransferGroupId.Value)
                                && t.Direction == TransactionDirection.Credit)
                    .Select(t => new { Gid = t.TransferGroupId!.Value, t.AccountId })
                    .ToListAsync(ct))
                .ToDictionary(x => x.Gid, x => x.AccountId);

            labelByAccount = (await db.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => new { a.AccountId, a.Nickname, a.BankCode, a.Last4 })
                    .ToListAsync(ct))
                .ToDictionary(a => a.AccountId, a => AccountLabel(a.Nickname, a.BankCode, a.Last4));
        }

        string? LabelOf(int? accountId) =>
            accountId is { } id && labelByAccount.TryGetValue(id, out var l) ? l : null;

        var items = rows.Select(r =>
        {
            string? from = null, to = null;
            if (r.TransferGroupId is { } gid)
            {
                from = LabelOf(r.AccountId);
                to = destAccountByGroup.TryGetValue(gid, out var destId) ? LabelOf(destId) : null;
            }
            return new TransactionDto(
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
                r.CreatedAt,
                r.TransferGroupId,
                from,
                to);
        }).ToList();

        return new PagedResult<TransactionDto>(items, page, pageSize, total);
    }

    /// <summary>A short display label for an account: its nickname, else "BANK ····1234".</summary>
    private static string AccountLabel(string? nickname, string? bankCode, string? last4)
    {
        if (!string.IsNullOrWhiteSpace(nickname))
            return nickname.Trim();
        var bank = string.IsNullOrWhiteSpace(bankCode) || bankCode == "GENERIC" ? "Card" : bankCode;
        return last4 is { Length: > 0 } ? $"{bank} ····{last4}" : bank;
    }
}
