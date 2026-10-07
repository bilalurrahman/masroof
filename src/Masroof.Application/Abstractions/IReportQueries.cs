using Masroof.Application.Reports;

namespace Masroof.Application.Abstractions;

/// <summary>
/// Fast, parameterized reporting aggregates (Dapper). Every method is scoped to a
/// <paramref name="userId"/> supplied by the server from the auth context — never by the
/// caller or the LLM. This is the only query surface the Ask tools reach.
/// </summary>
public interface IReportQueries
{
    Task<MonthlySummary> GetMonthlySummaryAsync(Guid userId, int year, int month, CancellationToken ct);

    Task<IReadOnlyList<TrendPoint>> GetTrendAsync(Guid userId, int months, CancellationToken ct);

    Task<IReadOnlyList<CategoryTotal>> GetSpendByCategoryAsync(Guid userId, int year, int month, CancellationToken ct);

    Task<IReadOnlyList<MerchantTotal>> GetTopMerchantsAsync(Guid userId, DateOnly from, DateOnly to, int n, CancellationToken ct);

    Task<MonthComparison> CompareMonthsAsync(Guid userId, int yearA, int monthA, int yearB, int monthB, CancellationToken ct);

    Task<IReadOnlyList<TxnLite>> FindTransactionsAsync(Guid userId, string query, int? year, int? month, int limit, CancellationToken ct);
}
