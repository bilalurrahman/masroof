namespace Masroof.Application.Common;

/// <summary>The allowed ISO-4217 currency codes. Unknown codes fail validation (422).</summary>
public static class Currencies
{
    public static readonly IReadOnlySet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "SAR", "USD", "EUR", "GBP", "AED", "EGP", "KWD", "BHD", "QAR", "OMR",
        "JOD", "PKR", "INR", "TRY", "CAD", "AUD"
    };

    public static bool IsAllowed(string? code) =>
        !string.IsNullOrWhiteSpace(code) && Allowed.Contains(code.Trim());
}
