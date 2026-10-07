using Masroof.Domain.Enums;

namespace Masroof.Application.Abstractions;

/// <summary>
/// Deterministic regex pre-parser. On a confident match it fills amount/direction/last4/
/// merchant, leaving only the category to rules or the LLM. Cheap, fast and explainable.
/// </summary>
public interface IPreParser
{
    /// <summary>Returns a result when some template matched the (already normalized) text, else null.</summary>
    PreParseResult? TryParse(string normalizedText);
}

/// <summary>
/// Fields a pre-parser template extracted. <see cref="Confidence"/> is 1.0 on a full
/// structural match; the caller decides whether that is enough to skip the LLM.
/// </summary>
public sealed record PreParseResult(
    string BankCode,
    TransactionDirection? Direction,
    decimal? Amount,
    string? Currency,
    string? Counterparty,
    string? Last4,
    string? Channel,
    DateOnly? Date,
    double Confidence);
