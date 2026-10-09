using System.Text;
using Masroof.Domain.Taxonomy;

namespace Masroof.Infrastructure.Llm;

/// <summary>Builds the versioned system prompt for parsing. Bump <see cref="Version"/> on any change.</summary>
public sealed class PromptBuilder
{
    /// <summary>Stored in ParseTraces so eval results are attributable to a prompt revision.</summary>
    public const string Version = "parse-v4";

    private const string FewShotExamples = """
        Input: "شراء بقيمة 87.50 ريال لدى بندة فرع العليا بطاقة تنتهي 1234"
        Output: {"direction":"debit","amount":87.50,"currency":"SAR","counterparty":"Panda","channel":"card","accountLast4":"1234","date":null,"category":"groceries","confidence":0.96}

        Input: "شراء بقيمة 120 ريال من كارفور بتاريخ 2026-10-04"
        Output: {"direction":"debit","amount":120,"currency":"SAR","counterparty":"Carrefour","channel":null,"accountLast4":null,"date":"2026-10-04","category":"groceries","confidence":0.95}

        Input: "Purchase SAR 23.00 at STARBUCKS card ending 1234"
        Output: {"direction":"debit","amount":23.00,"currency":"SAR","counterparty":"Starbucks","channel":"card","accountLast4":"1234","date":null,"category":"dining","confidence":0.95}

        Input: "POS Purchase SAR 45.00 at JARIR BOOKSTORE card ****5678"
        Output: {"direction":"debit","amount":45.00,"currency":"SAR","counterparty":"Jarir Bookstore","channel":"pos","accountLast4":"5678","date":null,"category":"shopping","confidence":0.9}

        Input: "Payment of SAR 180.00 to ALDREES fuel station"
        Output: {"direction":"debit","amount":180.00,"currency":"SAR","counterparty":"Aldrees","channel":null,"accountLast4":null,"date":null,"category":"fuel","confidence":0.94}

        Input: "دفعت 95 ريال لشركة الاتصالات STC باقة الجوال"
        Output: {"direction":"debit","amount":95,"currency":"SAR","counterparty":"STC","channel":null,"accountLast4":null,"date":null,"category":"telecom","confidence":0.95}

        Input: "ATM withdrawal SAR 500 Al Rajhi ATM"
        Output: {"direction":"debit","amount":500,"currency":"SAR","counterparty":"Al Rajhi","channel":"atm","accountLast4":null,"date":null,"category":"atm_cash","confidence":0.97}

        Input: "Careem trip SAR 32.50 charged to card 1234"
        Output: {"direction":"debit","amount":32.50,"currency":"SAR","counterparty":"Careem","channel":"card","accountLast4":"1234","date":null,"category":"transport","confidence":0.95}

        Input: "paid the plumber 150"
        Output: {"direction":"debit","amount":150,"currency":"SAR","counterparty":"plumber","channel":null,"accountLast4":null,"date":null,"category":"other","confidence":0.5}

        Input: "Salary credited SAR 12000.00 from ACME CORP"
        Output: {"direction":"credit","amount":12000.00,"currency":"SAR","counterparty":"Acme Corp","channel":"transfer","accountLast4":null,"date":null,"category":"salary","confidence":0.97}

        Input: "Refund SAR 60.00 from NOON reversed to your card"
        Output: {"direction":"credit","amount":60.00,"currency":"SAR","counterparty":"Noon","channel":"card","accountLast4":null,"date":null,"category":"refund","confidence":0.9}

        Input: "Payment of SAR 100.00 to Tabby installment 2 of 4"
        Output: {"direction":"debit","amount":100.00,"currency":"SAR","counterparty":"Tabby","channel":"bnpl","accountLast4":null,"date":null,"category":"bnpl","confidence":0.95}

        Input: "شراء 150 ريال عبر تمارا من نمشي"
        Output: {"direction":"debit","amount":150,"currency":"SAR","counterparty":"Namshi","channel":"bnpl","accountLast4":null,"date":null,"category":"shopping","confidence":0.9}
        """;

    public string Build(IReadOnlyList<string> hints, string defaultCurrency, DateOnly today)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You extract one financial transaction from a bank SMS, wallet notification, or a short note.");
        sb.AppendLine("Messages may be in Arabic, English, or mixed. Return only JSON matching the schema.");
        sb.AppendLine();
        sb.AppendLine("Fields:");
        sb.AppendLine("- direction: \"debit\" if money left the user (purchase, payment, withdrawal, transfer out), \"credit\" if it arrived (salary, refund, deposit, transfer in).");
        sb.AppendLine("- amount: number only, no currency symbols or thousands separators (e.g. 1234.50).");
        sb.AppendLine($"- currency: ISO 4217 code; default {defaultCurrency} if not stated (ريال/SR/ر.س ⇒ SAR).");
        sb.AppendLine("- counterparty: the merchant, biller, person, or employer — ALWAYS extract it when present. In Arabic the name usually follows من / لدى / في / إلى (from / at / to) — capture the name right after it. Use the clean brand/display name (e.g. \"Lulu Hypermarket\" ⇒ \"Lulu\", \"بندة\" ⇒ \"Panda\", \"كارفور\" ⇒ \"Carrefour\", \"البيك\" ⇒ \"Al Baik\"). Strip branch names (فرع …), card tails, and reference numbers. Translate well-known Arabic merchant names to their common English name; otherwise keep the original name. null only if truly absent.");
        sb.AppendLine("- channel: one of card, pos, atm, transfer, wallet, or null if unclear.");
        sb.AppendLine("- accountLast4: the last 4 digits of the card/account if present, else null.");
        sb.AppendLine($"- date: YYYY-MM-DD; if missing use {today:yyyy-MM-dd}.");
        sb.AppendLine("- category: choose the single best code from the list below based on the counterparty and context. Use \"other\" only when nothing fits.");
        sb.AppendLine("- confidence: 0.0-1.0, your certainty in the category.");
        sb.AppendLine();
        sb.AppendLine("Categories (code: what belongs here):");
        sb.AppendLine(CategoryGuidance.ToPromptList());
        sb.AppendLine();

        if (hints.Count > 0)
        {
            sb.AppendLine("Known rules for this user — these OVERRIDE the category guidance above when the named subject appears in the message:");
            foreach (var hint in hints)
                sb.AppendLine($"- {hint}");
            sb.AppendLine();
        }

        sb.AppendLine("Examples:");
        sb.AppendLine(FewShotExamples);
        return sb.ToString();
    }
}
