namespace Masroof.Domain.Enums;

/// <summary>How a transaction's fields were produced.</summary>
public enum TransactionSource
{
    /// <summary>Deterministic regex pre-parser produced the fields.</summary>
    PreParser,

    /// <summary>The LLM produced the fields.</summary>
    Llm,

    /// <summary>A human entered or corrected the fields.</summary>
    Manual
}

public static class TransactionSourceExtensions
{
    public static string ToDbValue(this TransactionSource source) => source switch
    {
        TransactionSource.PreParser => "preparser",
        TransactionSource.Llm => "llm",
        TransactionSource.Manual => "manual",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
    };

    public static TransactionSource ParseSource(string value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "preparser" => TransactionSource.PreParser,
            "llm" => TransactionSource.Llm,
            "manual" => TransactionSource.Manual,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown source.")
        };
}
