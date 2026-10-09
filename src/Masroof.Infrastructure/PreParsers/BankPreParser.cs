using System.Globalization;
using System.Text.RegularExpressions;
using Masroof.Application.Abstractions;
using Masroof.Domain.Enums;

namespace Masroof.Infrastructure.PreParsers;

/// <summary>
/// Heuristic, deterministic pre-parser for common Saudi bank SMS and wallet notifications
/// (Arabic + English). It fills amount/direction/last4/merchant; the category is always left
/// to rules or the LLM. Confidence is 1.0 only on a full structural match (amount + direction
/// + a card tail), which is what lets the pipeline skip the LLM.
/// </summary>
public sealed partial class BankPreParser : IPreParser
{
    public PreParseResult? TryParse(string normalizedText)
    {
        if (string.IsNullOrWhiteSpace(normalizedText))
            return null;

        var amount = ExtractAmount(normalizedText);
        var direction = ExtractDirection(normalizedText);
        if (amount is null || direction is null)
            return null; // not enough deterministic signal; hand to the LLM

        var last4 = ExtractLast4(normalizedText);
        var merchant = ExtractMerchant(normalizedText);
        var currency = ExtractCurrency(normalizedText);
        var channel = ExtractChannel(normalizedText);
        var bankCode = ExtractBankCode(normalizedText);

        // Full structural match ⇒ safe to skip the LLM (only the category remains).
        var confidence = last4 is not null && merchant is not null ? 1.0
                       : last4 is not null || merchant is not null ? 0.8
                       : 0.6;

        return new PreParseResult(
            BankCode: bankCode,
            Direction: direction,
            Amount: amount,
            Currency: currency,
            Counterparty: merchant,
            Last4: last4,
            Channel: channel,
            Date: ExtractDate(normalizedText),
            Confidence: confidence);
    }

    private static decimal? ExtractAmount(string text)
    {
        var m = AmountRegex().Match(text);
        if (!m.Success)
            return null;
        var raw = m.Groups["amt"].Value.Replace(",", "");
        return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) && amount > 0
            ? amount
            : null;
    }

    private static TransactionDirection? ExtractDirection(string text)
    {
        if (DebitRegex().IsMatch(text))
            return TransactionDirection.Debit;
        if (CreditRegex().IsMatch(text))
            return TransactionDirection.Credit;
        return null;
    }

    private static string? ExtractLast4(string text)
    {
        var m = Last4Regex().Match(text);
        return m.Success ? m.Groups["l4"].Value : null;
    }

    private static string? ExtractMerchant(string text)
    {
        var m = MerchantRegex().Match(text);
        if (!m.Success)
            return null;
        var name = m.Groups["mer"].Value.Trim().Trim('.', ',', '-', ';');
        return name.Length is >= 2 and <= 60 ? name : null;
    }

    private static string? ExtractCurrency(string text) =>
        SarRegex().IsMatch(text) ? "SAR"
        : UsdRegex().IsMatch(text) ? "USD"
        : AedRegex().IsMatch(text) ? "AED"
        : null;

    private static string? ExtractChannel(string text)
    {
        // BNPL providers are checked first: they are a distinct payment method and the card/pos
        // keywords in the same SMS should not mask them.
        if (BnplRegex().IsMatch(text)) return TransactionChannel.Bnpl;
        if (AtmRegex().IsMatch(text)) return TransactionChannel.Atm;
        if (PosRegex().IsMatch(text)) return TransactionChannel.Pos;
        if (TransferRegex().IsMatch(text)) return TransactionChannel.Transfer;
        if (CardRegex().IsMatch(text)) return TransactionChannel.Card;
        return null;
    }

    private static string ExtractBankCode(string text) =>
        RajhiRegex().IsMatch(text) ? "RAJHI"
        : SnbRegex().IsMatch(text) ? "SNB"
        : StcRegex().IsMatch(text) ? "STCPAY"
        : "GENERIC";

    private static DateOnly? ExtractDate(string text)
    {
        var m = DateRegex().Match(text);
        if (m.Success && DateOnly.TryParseExact(m.Groups["d"].Value, ["yyyy-MM-dd", "dd/MM/yyyy", "dd-MM-yyyy"],
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date;
        return null;
    }

    // Amount: optional currency token on either side of a number (digits already ASCII-normalized).
    [GeneratedRegex(@"(?:(?:SAR|SR|USD|AED|ر\.?\s?س|درهم|دولار)\s*)?(?<amt>\d{1,3}(?:,\d{3})*(?:\.\d{1,2})?|\d+(?:\.\d{1,2})?)\s*(?:SAR|SR|USD|AED|ر\.?\s?س|ريال|درهم|دولار)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b(debit|purchase|payment|paid|pos|withdraw(?:al|n)?|spent|charge)\b|شراء|سحب|خصم|دفع|مدين|نقاط\s*بيع",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DebitRegex();

    [GeneratedRegex(@"\b(credit|deposit|salary|received|refund|transfer\s+in|incoming)\b|إيداع|راتب|إضافة|دائن|حوالة\s*واردة|استرداد",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CreditRegex();

    [GeneratedRegex(@"(?:card|ending|acct|account|a/c|\*{2,}|x{2,}|بطاقة|حساب|تنتهي|منتهية)\D{0,12}?(?<l4>\d{4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex Last4Regex();

    [GeneratedRegex(@"(?:\bat\b|\bto\b|\bfrom\b|لدى|من|إلى|في)\s+(?<mer>[A-Za-z0-9&.\- ]{2,60}?)(?:\s+(?:card|on|بطاقة|بتاريخ|في)|[.,;]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MerchantRegex();

    [GeneratedRegex(@"\bSAR\b|\bSR\b|ر\.?\s?س|ريال", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SarRegex();

    [GeneratedRegex(@"\bUSD\b|دولار", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UsdRegex();

    [GeneratedRegex(@"\bAED\b|درهم", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AedRegex();

    [GeneratedRegex(@"\bATM\b|صراف", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex AtmRegex();

    [GeneratedRegex(@"\bPOS\b|نقاط\s*بيع", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PosRegex();

    [GeneratedRegex(@"transfer|حوالة|تحويل", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TransferRegex();

    // Buy-now-pay-later providers (Gulf): Tabby, Tamara, MisPay, Spotii, Postpay, Madfu.
    [GeneratedRegex(@"\btabby\b|\btamara\b|\bmispay\b|\bspotii\b|\bpostpay\b|\bmadfu\b|تابي|تمارا|ميس\s*باي|مدفوع",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BnplRegex();

    [GeneratedRegex(@"\bcard\b|بطاقة|مدى|mada|visa|mastercard", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CardRegex();

    [GeneratedRegex(@"الراجحي|rajhi", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex RajhiRegex();

    [GeneratedRegex(@"الأهلي|\bsnb\b|alahli", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SnbRegex();

    [GeneratedRegex(@"stc\s*pay|stcpay", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex StcRegex();

    [GeneratedRegex(@"(?<d>\d{4}-\d{2}-\d{2}|\d{2}/\d{2}/\d{4}|\d{2}-\d{2}-\d{4})", RegexOptions.Compiled)]
    private static partial Regex DateRegex();
}
