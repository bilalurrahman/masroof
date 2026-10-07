namespace Masroof.Api.Endpoints;

/// <summary>Request body for parsing one message.</summary>
public sealed record ParseRequest(string Text);

/// <summary>Request body for batch parsing (messages separated by a blank line).</summary>
public sealed record ParseBatchRequest(string Text);

/// <summary>Request body for editing a transaction. Only supplied fields change.</summary>
public sealed record PatchTransactionRequest(
    string? CategoryCode,
    decimal? Amount,
    DateOnly? TxnDate,
    string? Counterparty,
    string? Direction);

/// <summary>Request body for the Ask endpoint.</summary>
public sealed record AskHttpRequest(string Question);

/// <summary>202 response for an accepted background job.</summary>
public sealed record JobAccepted(long JobId, int Count);
