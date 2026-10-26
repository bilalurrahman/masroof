namespace Masroof.Application.Reports;

/// <summary>
/// The bank/wallet providers Masroof imports SMS from and monitors as a group:
/// D360, STC Bank, SABB, Alinma and tiqmo. Codes match <c>Account.BankCode</c> as written
/// by the SMS pre-parser; names are for display.
/// </summary>
public static class BankGroup
{
    /// <summary>Ordered (code, display name) pairs for the monitored banks.</summary>
    public static readonly IReadOnlyList<(string Code, string Name)> Members =
    [
        ("D360", "D360"),
        ("STCBANK", "STC Bank"),
        ("SABB", "SABB"),
        ("ALINMA", "Alinma"),
        ("TIQMO", "tiqmo"),
    ];

    public static readonly IReadOnlyList<string> Codes = Members.Select(m => m.Code).ToList();

    /// <summary>Display name for a bank code, falling back to the code itself.</summary>
    public static string NameOf(string? code) =>
        code is null ? "—" : Members.FirstOrDefault(m => m.Code == code).Name ?? code;
}
