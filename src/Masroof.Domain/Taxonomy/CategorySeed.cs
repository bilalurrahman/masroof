namespace Masroof.Domain.Taxonomy;

/// <summary>A seed definition for one taxonomy category (localized names, icon, color).</summary>
public sealed record CategorySeedItem(
    short CategoryId,
    string Code,
    string NameEn,
    string NameAr,
    string Icon,
    string Color);

/// <summary>
/// Canonical seed data for <c>dbo.Categories</c>. CategoryIds are fixed and must never
/// be reused or renumbered, since <c>Transactions.CategoryId</c> references them.
/// </summary>
public static class CategorySeed
{
    public static readonly IReadOnlyList<CategorySeedItem> Items =
    [
        new(1,  CategoryCodes.Groceries,      "Groceries",        "بقالة",            "shopping_cart",  "#4CAF50"),
        new(2,  CategoryCodes.Dining,         "Dining",           "مطاعم",            "restaurant",     "#FF7043"),
        new(3,  CategoryCodes.Transport,      "Transport",        "مواصلات",          "directions_bus", "#5C6BC0"),
        new(4,  CategoryCodes.Fuel,           "Fuel",             "وقود",             "local_gas_station","#8D6E63"),
        new(5,  CategoryCodes.Utilities,      "Utilities",        "خدمات",            "bolt",           "#FBC02D"),
        new(6,  CategoryCodes.Telecom,        "Telecom",          "اتصالات",          "sim_card",       "#26A69A"),
        new(7,  CategoryCodes.Rent,           "Rent",             "إيجار",            "home",           "#7E57C2"),
        new(8,  CategoryCodes.Health,         "Health",           "صحة",              "local_hospital", "#EF5350"),
        new(9,  CategoryCodes.Education,      "Education",        "تعليم",            "school",         "#42A5F5"),
        new(10, CategoryCodes.Shopping,       "Shopping",         "تسوق",             "shopping_bag",   "#EC407A"),
        new(11, CategoryCodes.Entertainment,  "Entertainment",    "ترفيه",            "movie",          "#AB47BC"),
        new(12, CategoryCodes.Travel,         "Travel",           "سفر",              "flight",         "#29B6F6"),
        new(13, CategoryCodes.FamilyTransfer, "Family Transfer",  "تحويل عائلي",      "family_restroom","#66BB6A"),
        new(14, CategoryCodes.Salary,         "Salary",           "راتب",             "payments",       "#9CCC65"),
        new(15, CategoryCodes.Refund,         "Refund",           "استرداد",          "undo",           "#26C6DA"),
        new(16, CategoryCodes.FeesCharges,    "Fees & Charges",   "رسوم",             "receipt_long",   "#BDBDBD"),
        new(17, CategoryCodes.AtmCash,        "ATM / Cash",       "صراف / نقد",       "local_atm",      "#78909C"),
        new(18, CategoryCodes.Investment,     "Investment",       "استثمار",          "trending_up",    "#2E7D32"),
        new(19, CategoryCodes.Government,     "Government",       "حكومي",            "account_balance","#546E7A"),
        new(20, CategoryCodes.Other,          "Other",            "أخرى",             "category",       "#90A4AE"),
    ];

    public static short IdFor(string code) =>
        Items.FirstOrDefault(i => string.Equals(i.Code, code, StringComparison.OrdinalIgnoreCase))?.CategoryId
        ?? Items.First(i => i.Code == CategoryCodes.Other).CategoryId;
}
