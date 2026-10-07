using Masroof.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Categories;

/// <summary>A taxonomy category, localized for the current user, for pickers and chips.</summary>
public sealed record CategoryDto(string Code, string Name, string? Icon, string? Color);

/// <summary>Returns the full taxonomy localized to the current user's locale.</summary>
public sealed class GetCategoriesHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<CategoryDto>> HandleAsync(CancellationToken ct)
    {
        var isArabic = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var rows = await db.Categories
            .OrderBy(c => c.CategoryId)
            .Select(c => new { c.Code, c.NameEn, c.NameAr, c.Icon, c.Color })
            .ToListAsync(ct);

        return rows.Select(c => new CategoryDto(
            c.Code, isArabic ? c.NameAr : c.NameEn, c.Icon, c.Color)).ToList();
    }
}
