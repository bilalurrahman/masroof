using Dapper;
using Masroof.Application.Abstractions;
using Masroof.Application.Reports;

namespace Masroof.Infrastructure.Persistence;

/// <summary>
/// Dapper implementation of the reporting aggregates. Every query is parameterized and
/// filtered by <c>UserId</c> and <c>IsDeleted = 0</c>. Category names are localized to the
/// current user's locale.
/// </summary>
public sealed class ReportQueries(ISqlConnectionFactory factory, ICurrentUser currentUser, IClock clock)
    : IReportQueries
{
    private bool IsArabic => currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    public async Task<MonthlySummary> GetMonthlySummaryAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);
        var prevFrom = from.AddMonths(-1);

        await using var conn = await factory.OpenAsync(ct);

        var totals = await conn.QuerySingleAsync<(decimal Debit, decimal Credit, int Cnt)>(new CommandDefinition(
            """
            SELECT
              COALESCE(SUM(CASE WHEN Direction='debit'  THEN Amount END),0) AS Debit,
              COALESCE(SUM(CASE WHEN Direction='credit' THEN Amount END),0) AS Credit,
              COUNT(*) AS Cnt
            FROM dbo.Transactions
            WHERE UserId=@userId AND IsDeleted=0 AND TxnDate>=@from AND TxnDate<@to;
            """, new { userId, from, to }, cancellationToken: ct));

        var prevDebit = await conn.ExecuteScalarAsync<decimal?>(new CommandDefinition(
            """
            SELECT COALESCE(SUM(CASE WHEN Direction='debit' THEN Amount END),0)
            FROM dbo.Transactions
            WHERE UserId=@userId AND IsDeleted=0 AND TxnDate>=@prevFrom AND TxnDate<@from;
            """, new { userId, prevFrom, from }, cancellationToken: ct)) ?? 0m;

        var byCategory = await GetSpendByCategoryAsync(userId, year, month, ct);

        return new MonthlySummary(
            Month: $"{year:D4}-{month:D2}",
            TotalDebit: totals.Debit,
            TotalCredit: totals.Credit,
            TransactionCount: totals.Cnt,
            PreviousMonthDebit: prevDebit,
            ByCategory: byCategory);
    }

    public async Task<IReadOnlyList<TrendPoint>> GetTrendAsync(Guid userId, int months, CancellationToken ct)
    {
        var n = Math.Clamp(months, 1, 36);
        var today = clock.Today;
        var start = new DateOnly(today.Year, today.Month, 1).AddMonths(-(n - 1));

        await using var conn = await factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<(string Month, decimal Debit, decimal Credit)>(new CommandDefinition(
            """
            SELECT FORMAT(TxnDate,'yyyy-MM') AS Month,
                   COALESCE(SUM(CASE WHEN Direction='debit'  THEN Amount END),0) AS Debit,
                   COALESCE(SUM(CASE WHEN Direction='credit' THEN Amount END),0) AS Credit
            FROM dbo.Transactions
            WHERE UserId=@userId AND IsDeleted=0 AND TxnDate>=@start
            GROUP BY FORMAT(TxnDate,'yyyy-MM')
            ORDER BY Month;
            """, new { userId, start }, cancellationToken: ct));

        return rows.Select(r => new TrendPoint(r.Month, r.Debit, r.Credit)).ToList();
    }

    public async Task<IReadOnlyList<CategoryTotal>> GetSpendByCategoryAsync(Guid userId, int year, int month, CancellationToken ct)
    {
        var from = new DateOnly(year, month, 1);
        var to = from.AddMonths(1);

        await using var conn = await factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<CategoryRow>(new CommandDefinition(
            """
            SELECT c.Code, c.NameEn, c.NameAr, SUM(t.Amount) AS Total, COUNT(*) AS Cnt
            FROM dbo.Transactions t
            JOIN dbo.Categories c ON c.CategoryId = t.CategoryId
            WHERE t.UserId=@userId AND t.IsDeleted=0 AND t.Direction='debit'
                  AND t.TxnDate>=@from AND t.TxnDate<@to
            GROUP BY c.Code, c.NameEn, c.NameAr
            ORDER BY Total DESC;
            """, new { userId, from, to }, cancellationToken: ct));

        return rows.Select(r => new CategoryTotal(r.Code, IsArabic ? r.NameAr : r.NameEn, r.Total, r.Cnt)).ToList();
    }

    public async Task<IReadOnlyList<MerchantTotal>> GetTopMerchantsAsync(Guid userId, DateOnly from, DateOnly to, int n, CancellationToken ct)
    {
        var top = Math.Clamp(n, 1, 50);
        await using var conn = await factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<(string Counterparty, decimal Total, int Cnt)>(new CommandDefinition(
            """
            SELECT TOP (@top) COALESCE(Counterparty,'(unknown)') AS Counterparty,
                   SUM(Amount) AS Total, COUNT(*) AS Cnt
            FROM dbo.Transactions
            WHERE UserId=@userId AND IsDeleted=0 AND Direction='debit'
                  AND TxnDate>=@from AND TxnDate<=@to
            GROUP BY COALESCE(Counterparty,'(unknown)')
            ORDER BY Total DESC;
            """, new { userId, from, to, top }, cancellationToken: ct));

        return rows.Select(r => new MerchantTotal(r.Counterparty, r.Total, r.Cnt)).ToList();
    }

    public async Task<MonthComparison> CompareMonthsAsync(Guid userId, int yearA, int monthA, int yearB, int monthB, CancellationToken ct)
    {
        var a = await GetSpendByCategoryAsync(userId, yearA, monthA, ct);
        var b = await GetSpendByCategoryAsync(userId, yearB, monthB, ct);

        var byCode = new Dictionary<string, (string Name, decimal A, decimal B)>();
        foreach (var x in a)
            byCode[x.CategoryCode] = (x.CategoryName, x.Total, 0m);
        foreach (var y in b)
            byCode[y.CategoryCode] = byCode.TryGetValue(y.CategoryCode, out var e)
                ? (e.Name, e.A, y.Total)
                : (y.CategoryName, 0m, y.Total);

        var deltas = byCode
            .Select(kv => new CategoryDelta(kv.Key, kv.Value.Name, kv.Value.A, kv.Value.B))
            .OrderByDescending(d => d.TotalB)
            .ToList();

        return new MonthComparison(
            $"{yearA:D4}-{monthA:D2}",
            $"{yearB:D4}-{monthB:D2}",
            a.Sum(x => x.Total),
            b.Sum(x => x.Total),
            deltas);
    }

    public async Task<IReadOnlyList<TxnLite>> FindTransactionsAsync(Guid userId, string query, int? year, int? month, int limit, CancellationToken ct)
    {
        var top = Math.Clamp(limit, 1, 100);
        var like = $"%{query.Trim()}%";

        DateOnly? from = null, to = null;
        if (year is { } y && month is { } m)
        {
            from = new DateOnly(y, m, 1);
            to = from.Value.AddMonths(1);
        }

        await using var conn = await factory.OpenAsync(ct);
        var rows = await conn.QueryAsync<TxnLiteRow>(new CommandDefinition(
            """
            SELECT TOP (@top) t.TransactionId AS Id, t.TxnDate AS Date, t.Amount, t.Direction,
                   t.Counterparty, c.Code AS CategoryCode
            FROM dbo.Transactions t
            JOIN dbo.Categories c ON c.CategoryId = t.CategoryId
            WHERE t.UserId=@userId AND t.IsDeleted=0
                  AND (t.Counterparty LIKE @like OR t.RawText LIKE @like)
                  AND (@from IS NULL OR (t.TxnDate>=@from AND t.TxnDate<@to))
            ORDER BY t.TxnDate DESC, t.TransactionId DESC;
            """, new { userId, like, from, to, top }, cancellationToken: ct));

        return rows.Select(r => new TxnLite(r.Id, r.Date, r.Amount, r.Direction, r.Counterparty, r.CategoryCode)).ToList();
    }

    private sealed record CategoryRow(string Code, string NameEn, string NameAr, decimal Total, int Cnt);
    private sealed record TxnLiteRow(long Id, DateOnly Date, decimal Amount, string Direction, string? Counterparty, string CategoryCode);
}
