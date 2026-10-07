namespace Masroof.Application.Reports;

/// <summary>Total debit spend for one category over a period.</summary>
public sealed record CategoryTotal(string CategoryCode, string CategoryName, decimal Total, int Count);

/// <summary>Total debit spend for one merchant over a period.</summary>
public sealed record MerchantTotal(string Counterparty, decimal Total, int Count);

/// <summary>One month of the trend series: total debits and credits.</summary>
public sealed record TrendPoint(string Month, decimal Debit, decimal Credit);

/// <summary>A compact transaction row for search results and "data used" tables.</summary>
public sealed record TxnLite(
    long Id,
    DateOnly Date,
    decimal Amount,
    string Direction,
    string? Counterparty,
    string CategoryCode);

/// <summary>Per-category delta between two months.</summary>
public sealed record CategoryDelta(string CategoryCode, string CategoryName, decimal TotalA, decimal TotalB);

/// <summary>Overall and per-category comparison of two months.</summary>
public sealed record MonthComparison(
    string MonthA,
    string MonthB,
    decimal TotalDebitA,
    decimal TotalDebitB,
    IReadOnlyList<CategoryDelta> ByCategory);

/// <summary>The summary report for one month: totals, breakdown, and change vs last month.</summary>
public sealed record MonthlySummary(
    string Month,
    decimal TotalDebit,
    decimal TotalCredit,
    int TransactionCount,
    decimal PreviousMonthDebit,
    IReadOnlyList<CategoryTotal> ByCategory);
