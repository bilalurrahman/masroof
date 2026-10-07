namespace Masroof.Application.Abstractions;

/// <summary>Turns a free-form message into a structured transaction via a constrained-output LLM.</summary>
public interface ILlmTransactionParser
{
    Task<ParseResult> ParseAsync(string text, IReadOnlyList<string> hints, string defaultCurrency, CancellationToken ct);
}

/// <summary>The structured fields the model extracts (schema-constrained).</summary>
public sealed record ParsedTransaction(
    string Direction,
    decimal Amount,
    string Currency,
    string? Counterparty,
    string? Channel,
    string? AccountLast4,
    DateOnly? Date,
    string Category,
    double Confidence);

/// <summary>
/// The full outcome of a parse call, including observability fields persisted to
/// <c>ParseTraces</c>. <see cref="Succeeded"/> is false when the model was unreachable
/// or its output could not be bound to <see cref="Transaction"/>.
/// </summary>
public sealed record ParseResult(
    bool Succeeded,
    ParsedTransaction? Transaction,
    string Model,
    string PromptVersion,
    string? RawOutput,
    int LatencyMs,
    string? Error);
