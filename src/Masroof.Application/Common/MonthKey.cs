using System.Globalization;

namespace Masroof.Application.Common;

/// <summary>Parses and represents a "yyyy-MM" month key used across reports and Ask tools.</summary>
public readonly record struct MonthKey(int Year, int Month)
{
    public DateOnly Start => new(Year, Month, 1);
    public DateOnly EndExclusive => Start.AddMonths(1);
    public override string ToString() => $"{Year:D4}-{Month:D2}";

    public static MonthKey Parse(string value)
    {
        if (TryParse(value, out var key))
            return key;
        throw new FormatException($"Invalid month '{value}'. Expected format yyyy-MM.");
    }

    public static bool TryParse(string? value, out MonthKey key)
    {
        key = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (DateTime.TryParseExact(value.Trim() + "-01", "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            key = new MonthKey(dt.Year, dt.Month);
            return true;
        }
        return false;
    }

    public static MonthKey FromDate(DateOnly date) => new(date.Year, date.Month);
}
