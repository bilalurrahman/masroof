namespace Masroof.Domain.Enums;

/// <summary>
/// Known payment channels. Stored as a nullable string on <see cref="Entities.Transaction"/>
/// because the set is open-ended and frequently unknown from the source message.
/// </summary>
public static class TransactionChannel
{
    public const string Card = "card";
    public const string Transfer = "transfer";
    public const string Wallet = "wallet";
    public const string Atm = "atm";
    public const string Pos = "pos";
    public const string Online = "online";

    /// <summary>Buy-now-pay-later provider (Tabby, Tamara, …) — a payment method, not a category.</summary>
    public const string Bnpl = "bnpl";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Card, Transfer, Wallet, Atm, Pos, Online, Bnpl
    };

    /// <summary>Returns the normalized channel if recognized, otherwise null.</summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var v = value.Trim().ToLowerInvariant();
        return All.Contains(v) ? v : null;
    }
}
