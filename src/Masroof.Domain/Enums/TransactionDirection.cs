namespace Masroof.Domain.Enums;

/// <summary>Whether money left the user (debit) or arrived (credit).</summary>
public enum TransactionDirection
{
    Debit,
    Credit
}

public static class TransactionDirectionExtensions
{
    public static string ToDbValue(this TransactionDirection direction) =>
        direction == TransactionDirection.Credit ? "credit" : "debit";

    public static TransactionDirection ParseDirection(string value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "credit" => TransactionDirection.Credit,
            "debit" => TransactionDirection.Debit,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown direction.")
        };
}
