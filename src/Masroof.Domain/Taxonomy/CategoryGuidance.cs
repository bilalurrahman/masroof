namespace Masroof.Domain.Taxonomy;

/// <summary>
/// Human-readable guidance for each taxonomy code: a short definition plus example
/// merchants (Gulf/Saudi-centric, Arabic + English) that the parse prompt lists so the
/// model picks the *right* category, not just a structurally valid one. Keep in lockstep
/// with <see cref="CategoryCodes"/> — every code must have an entry (asserted in tests).
/// </summary>
public static class CategoryGuidance
{
    public static readonly IReadOnlyDictionary<string, string> Descriptions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [CategoryCodes.Groceries] = "supermarkets & grocery stores (Panda/بندة, Carrefour/كارفور, Lulu/لولو, Danube/الدانوب, Tamimi/التميمي, Othaim/العثيم, Nesto/نستو, بقالة, تموينات, سوبرماركت)",
            [CategoryCodes.Dining] = "restaurants, cafés, coffee shops, fast food & food delivery (Starbucks/ستاربكس, McDonald's/ماكدونالدز, Herfy/هرفي, Al Baik/البيك, Kudo/كودو, Dunkin/دانكن, Jahez/جاهز, HungerStation/هنقرستيشن, مطعم, كافيه, قهوة)",
            [CategoryCodes.Transport] = "ride-hailing, taxi, bus, metro, parking & tolls (Uber/أوبر, Careem/كريم, Jeeny/جيني, SAPTCO, مواصلات, أجرة, تاكسي) — NOT petrol",
            [CategoryCodes.Fuel] = "petrol / gas stations (Aldrees/الدريس, Sasco/ساسكو, Petromin/بترومين, محطة وقود, بنزين, محطة)",
            [CategoryCodes.Utilities] = "electricity, water & gas bills (SEC/الكهرباء, NWC/المياه, كهرباء, مياه, فاتورة)",
            [CategoryCodes.Telecom] = "mobile, internet & landline (STC/إس تي سي, Mobily/موبايلي, Zain/زين, Salam/سلام, اتصالات, انترنت, باقة)",
            [CategoryCodes.Rent] = "housing rent & Ejar payments (إيجار, اجار, Ejar)",
            [CategoryCodes.Health] = "pharmacies, clinics, hospitals & labs (Nahdi/النهدي, Al-Dawaa/الدواء, Dr. Sulaiman Al Habib/سليمان الحبيب, صيدلية, مستشفى, عيادة)",
            [CategoryCodes.Education] = "schools, universities, courses & tuition (مدرسة, جامعة, دورة, رسوم دراسية, tuition)",
            [CategoryCodes.Shopping] = "retail goods — electronics, clothing, books, furniture, general merchandise (Jarir/جرير, Extra/إكسترا, Noon/نون, Amazon/أمازون, IKEA/ايكيا, SACO/ساكو, Centrepoint, تسوق)",
            [CategoryCodes.Entertainment] = "streaming, games, cinema & events (Netflix, Shahid, Spotify, PlayStation, VOX/AMC cinema, سينما, ألعاب)",
            [CategoryCodes.Travel] = "flights, hotels & car rental (Saudia, Flynas, flyadeal, Booking, Almosafer, فندق, طيران, تذكرة)",
            [CategoryCodes.FamilyTransfer] = "transfers to family members or individuals, and remittances (تحويل إلى, حوالة, remittance to a person)",
            [CategoryCodes.Salary] = "incoming salary or payroll (راتب, salary credited, payroll)",
            [CategoryCodes.Refund] = "money returned, reversed or refunded (استرداد, استرجاع, reversal, refund)",
            [CategoryCodes.FeesCharges] = "bank fees, service charges, commissions, VAT & interest (رسوم, عمولة, ضريبة)",
            [CategoryCodes.AtmCash] = "ATM withdrawals & cash (سحب نقدي, صراف آلي, ATM withdrawal)",
            [CategoryCodes.Investment] = "brokerage, stocks, funds & crypto (تداول, استثمار, صندوق, أسهم)",
            [CategoryCodes.Government] = "government services, fines & traffic violations (Absher, Sadad government, Muqeem, مخالفات, رسوم حكومية)",
            [CategoryCodes.TransferInternal] = "moving money between the user's OWN accounts (e.g. salary account → budget account, card top-up from own account); not spending. NOT remittances to other people (that is family_transfer)",
            [CategoryCodes.Other] = "anything that does not clearly fit another category",
        };

    /// <summary>Renders "- code: description" lines in <see cref="CategoryCodes.All"/> order.</summary>
    public static string ToPromptList()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var code in CategoryCodes.All)
            sb.Append("- ").Append(code).Append(": ").AppendLine(Descriptions[code]);
        return sb.ToString().TrimEnd();
    }
}
