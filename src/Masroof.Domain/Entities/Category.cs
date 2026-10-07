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
}
