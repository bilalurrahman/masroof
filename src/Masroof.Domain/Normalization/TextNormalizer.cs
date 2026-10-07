using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Masroof.Domain.Normalization;

/// <summary>
/// Deterministic text normalization shared by the pre-parser, the rules engine and
/// entity creation. Keeping it in one place means dedupe hashing and subject matching
/// always agree on what "the same text" means.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Normalizes raw user input: unifies Arabic-Indic and Extended Arabic-Indic digits
    /// to ASCII, trims, and collapses internal whitespace runs to a single space.
    /// </summary>
    public static string NormalizeInput(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var ch in text.Trim())
        {
            var mapped = MapDigit(ch);
            if (char.IsWhiteSpace(mapped))
            {
                if (!lastWasSpace)
                    sb.Append(' ');
                lastWasSpace = true;
            }
            else
            {
                sb.Append(mapped);
                lastWasSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>
    /// Normalizes a merchant / counterparty name for exact and fuzzy matching:
    /// uppercase, digits unified, punctuation removed, whitespace collapsed.
    /// </summary>
    public static string NormalizeSubject(string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
            return string.Empty;

        var sb = new StringBuilder(subject.Length);
        var lastWasSpace = false;
        foreach (var raw in subject.Trim())
        {
            var ch = MapDigit(raw);
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToUpper(ch, CultureInfo.InvariantCulture));
                lastWasSpace = false;
            }
            else if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsSymbol(ch))
            {
                if (!lastWasSpace)
                    sb.Append(' ');
                lastWasSpace = true;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>Splits a normalized subject into its word tokens.</summary>
    public static string[] Tokenize(string? normalized) =>
        string.IsNullOrWhiteSpace(normalized)
            ? []
            : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>SHA-256 of the normalized input, used for the dedupe constraint.</summary>
    public static byte[] Sha256(string? text)
    {
        var normalized = NormalizeInput(text);
        return SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
    }

    private static char MapDigit(char ch) => ch switch
    {
        // Arabic-Indic digits ٠-٩
        >= '٠' and <= '٩' => (char)('0' + (ch - '٠')),
        // Extended Arabic-Indic (Persian/Urdu) digits ۰-۹
        >= '۰' and <= '۹' => (char)('0' + (ch - '۰')),
        _ => ch
    };
}
