namespace Masroof.Application.Transactions;

/// <summary>A localized category reference returned to clients.</summary>
public sealed record CategoryRef(string Code, string Name);

/// <summary>A full ledger row as returned by the API.</summary>
public sealed record TransactionDto(
    long Id,
    string Direction,
    decimal Amount,
    string Currency,
    string? Counterparty,
    string? Channel,
    DateOnly TxnDate,
    CategoryRef Category,
    decimal? Confidence,
    string Source,
    bool IsCorrected,
    DateTime CreatedAt);

/// <summary>The parse response: the saved row plus the hints that influenced it and latency.</summary>
public sealed record ParseResponse(
    long Id,
    string Direction,
    decimal Amount,
    string Currency,
    string? Counterparty,
    string? Channel,
    DateOnly TxnDate,
    CategoryRef Category,
    decimal? Confidence,
    string Source,
    IReadOnlyList<string> HintsUsed,
    int LatencyMs);
