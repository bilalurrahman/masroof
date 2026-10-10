namespace Masroof.Infrastructure.Sms;

/// <summary>Configuration for reading bank/wallet SMS from the host device.</summary>
public sealed class SmsInboxOptions
{
    public const string SectionName = "SmsInbox";

    /// <summary>
    /// Path to the macOS Messages SQLite database. Defaults to the signed-in user's
    /// <c>~/Library/Messages/chat.db</c>. Reading it requires the host process to have
    /// Full Disk Access (macOS Privacy &amp; Security).
    /// </summary>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Case-insensitive substrings matched against the SMS sender id. A message is ingested when
    /// its sender contains any of these. Defaults cover SABB, Alinma, D360, STC Pay and tiqmo
    /// (Latin and common Arabic spellings).
    /// </summary>
    public string[] Providers { get; set; } =
    [
        "SABB", "SAB", "ساب",
        "Alinma", "الانماء", "الإنماء", "إنماء",
        "D360", "دي360",
        "STCPay", "STC Pay", "STCPAY", "stc pay", "STC Bank", "اس تي سي",
        "tiqmo", "Tiqmo", "تيكمو", "تكمو"
    ];
}
