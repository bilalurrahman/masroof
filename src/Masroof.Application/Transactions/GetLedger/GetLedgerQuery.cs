namespace Masroof.Application.Transactions.GetLedger;

/// <summary>Filters for the paged ledger. <see cref="NeedsReview"/> selects low-confidence rows.</summary>
public sealed record GetLedgerQuery(
    int? Year = null,
    int? Month = null,
    string? CategoryCode = null,
    string? Direction = null,
    int? AccountId = null,
    string? Search = null,
    bool NeedsReview = false,
    int Page = 1,
    int PageSize = 50);
