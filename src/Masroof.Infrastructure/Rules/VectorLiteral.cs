using System.Globalization;
using System.Text;

namespace Masroof.Infrastructure.Rules;

/// <summary>Serializes a float[] to the JSON-array string SQL Server casts to its VECTOR type.</summary>
public static class VectorLiteral
{
    public static string From(float[] vector)
    {
        var sb = new StringBuilder(vector.Length * 8 + 2);
        sb.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }
}
