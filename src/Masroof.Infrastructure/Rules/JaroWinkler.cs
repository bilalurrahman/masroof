namespace Masroof.Infrastructure.Rules;

/// <summary>Jaro-Winkler similarity in [0,1]. Used for the fuzzy rule-matching tier (≥ 0.92).</summary>
public static class JaroWinkler
{
    public static double Similarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0.0;
        if (s1 == s2)
            return 1.0;

        var jaro = Jaro(s1, s2);

        // Winkler boost for a common prefix up to 4 chars.
        var prefix = 0;
        var max = Math.Min(4, Math.Min(s1.Length, s2.Length));
        while (prefix < max && s1[prefix] == s2[prefix])
            prefix++;

        return jaro + prefix * 0.1 * (1 - jaro);
    }

    private static double Jaro(string s1, string s2)
    {
        var matchDistance = Math.Max(s1.Length, s2.Length) / 2 - 1;
        if (matchDistance < 0) matchDistance = 0;

        var s1Matches = new bool[s1.Length];
        var s2Matches = new bool[s2.Length];
        var matches = 0;

        for (var i = 0; i < s1.Length; i++)
        {
            var start = Math.Max(0, i - matchDistance);
            var end = Math.Min(i + matchDistance + 1, s2.Length);
            for (var j = start; j < end; j++)
            {
                if (s2Matches[j] || s1[i] != s2[j]) continue;
                s1Matches[i] = true;
                s2Matches[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
            return 0.0;

        double transpositions = 0;
        var k = 0;
        for (var i = 0; i < s1.Length; i++)
        {
            if (!s1Matches[i]) continue;
            while (!s2Matches[k]) k++;
            if (s1[i] != s2[k]) transpositions++;
            k++;
        }
        transpositions /= 2;

        return (matches / (double)s1.Length
                + matches / (double)s2.Length
                + (matches - transpositions) / matches) / 3.0;
    }
}
