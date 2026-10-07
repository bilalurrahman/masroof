using System.Text;

namespace Masroof.Infrastructure.Llm;

/// <summary>Builds the versioned system prompt for parsing. Bump <see cref="Version"/> on any change.</summary>
public sealed class PromptBuilder
{
    /// <summary>Stored in ParseTraces so eval results are attributable to a prompt revision.</summary>
    public const string Version = "parse-v1";

    private const string FewShotExamples = """
        Input: "شراء بقيمة 87.50 ريال لدى بندة فرع العليا بطاقة تنتهي 1234"
        Output: {"direction":"debit","amount":87.50,"currency":"SAR","counterparty":"Panda","channel":"card","accountLast4":"1234","date":null,"category":"groceries","confidence":0.95}

        Input: "POS Purchase SAR 45.00 at JARIR BOOKSTORE card ****5678"
        Output: {"direction":"debit","amount":45.00,"currency":"SAR","counterparty":"Jarir Bookstore","channel":"pos","accountLast4":"5678","date":null,"category":"shopping","confidence":0.9}

        Input: "paid the plumber 150"
        Output: {"direction":"debit","amount":150,"currency":"SAR","counterparty":"plumber","channel":null,"accountLast4":null,"date":null,"category":"other","confidence":0.5}

        Input: "Salary credited SAR 12000.00 from ACME CORP"
        Output: {"direction":"credit","amount":12000.00,"currency":"SAR","counterparty":"Acme Corp","channel":"transfer","accountLast4":null,"date":null,"category":"salary","confidence":0.97}
        """;

    public string Build(IReadOnlyList<string> hints, string defaultCurrency, DateOnly today)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You extract one financial transaction from a bank SMS, wallet notification, or a short note.");
        sb.AppendLine("Messages may be in Arabic, English, or mixed. Return only JSON matching the schema.");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- direction: \"debit\" if money left the user, \"credit\" if it arrived.");
        sb.AppendLine("- amount: number only, no currency symbols or thousands separators.");
        sb.AppendLine($"- currency: ISO code; default {defaultCurrency} if not stated.");
        sb.AppendLine($"- date: YYYY-MM-DD; if missing use {today:yyyy-MM-dd}.");
        sb.AppendLine("- category: one of the allowed codes. Use \"other\" if unsure.");
        sb.AppendLine("- confidence: 0.0-1.0, your certainty in the category.");
        sb.AppendLine();

        if (hints.Count > 0)
        {
            sb.AppendLine("Known rules for this user (follow them when the subject appears in the message):");
            foreach (var hint in hints)
                sb.AppendLine($"- {hint}");
            sb.AppendLine();
        }

        sb.AppendLine("Examples:");
        sb.AppendLine(FewShotExamples);
        return sb.ToString();
    }
}
