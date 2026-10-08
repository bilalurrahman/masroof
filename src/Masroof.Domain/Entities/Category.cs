namespace Masroof.Domain.Entities;

/// <summary>A taxonomy category. Seeded from <see cref="Taxonomy.CategorySeed"/>; not user-editable.</summary>
public class Category
{
    public short CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? Color { get; set; }

    /// <summary>
    /// When true, transactions in this category are kept for the audit trail but excluded
    /// from spend/income totals and reports — used for internal money movement between the
    /// user's own accounts, which is not real spending or income.
    /// </summary>
    public bool ExcludeFromTotals { get; set; }
}
