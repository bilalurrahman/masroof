namespace Masroof.Domain.Taxonomy;

/// <summary>
/// The fixed expense/income taxonomy. These codes are the single source of truth:
/// the LLM JSON schema enumerates them, the DB seeds them, and rules reference them.
/// Keep this list small and stable — changing it is a schema + prompt change.
/// </summary>
public static class CategoryCodes
{
    public const string Groceries = "groceries";
    public const string Dining = "dining";
    public const string Transport = "transport";
    public const string Fuel = "fuel";
    public const string Utilities = "utilities";
    public const string Telecom = "telecom";
    public const string Rent = "rent";
    public const string Health = "health";
    public const string Education = "education";
    public const string Shopping = "shopping";
    public const string Entertainment = "entertainment";
    public const string Travel = "travel";
    public const string FamilyTransfer = "family_transfer";
    public const string Salary = "salary";
    public const string Refund = "refund";
    public const string FeesCharges = "fees_charges";
    public const string AtmCash = "atm_cash";
    public const string Investment = "investment";
    public const string Government = "government";
    public const string Other = "other";

    /// <summary>All taxonomy codes in a stable, display order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Groceries, Dining, Transport, Fuel, Utilities, Telecom, Rent, Health,
        Education, Shopping, Entertainment, Travel, FamilyTransfer, Salary,
        Refund, FeesCharges, AtmCash, Investment, Government, Other
    ];

    private static readonly HashSet<string> AllSet = new(All, StringComparer.OrdinalIgnoreCase);

    public static bool IsValid(string? code) =>
        !string.IsNullOrWhiteSpace(code) && AllSet.Contains(code.Trim());

    /// <summary>Returns the normalized code if valid, otherwise <see cref="Other"/>.</summary>
    public static string ResolveOrOther(string? code) =>
        IsValid(code) ? code!.Trim().ToLowerInvariant() : Other;
}
