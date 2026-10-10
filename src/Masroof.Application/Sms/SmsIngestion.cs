using System.Text.RegularExpressions;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Application.Transactions.ParseTransaction;
using Microsoft.Extensions.Logging;

namespace Masroof.Application.Sms;

/// <summary>Outcome of one ingestion run.</summary>
/// <param name="Scanned">Messages read from the inbox for the window.</param>
/// <param name="Imported">New ledger rows created.</param>
/// <param name="Duplicates">Messages skipped because an identical row already exists.</param>
/// <param name="Failed">Messages that could not be parsed into a row.</param>
public sealed record SmsIngestionResult(int Scanned, int Imported, int Duplicates, int Failed);

/// <summary>
/// Reads bank/wallet SMS for a given window from <see cref="ISmsInbox"/> and runs each message
/// through the normal parse pipeline (<see cref="ParseTransactionHandler"/>), so dedupe, the
/// pre-parser, rules, the LLM and persistence all behave exactly as they do for a pasted message.
/// Re-running over the same window is safe: already-captured messages surface as duplicates and
/// are skipped. The caller is responsible for setting the current user before invoking this.
/// </summary>
public sealed class IngestSmsHandler(
    ISmsInbox inbox,
    ParseTransactionHandler parser,
    ILogger<IngestSmsHandler> logger)
{
    public async Task<SmsIngestionResult> HandleAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct)
    {
        var messages = await inbox.ReadAsync(fromUtc, toUtc, ct);
        int imported = 0, duplicates = 0, failed = 0, skipped = 0;

        foreach (var sms in messages)
        {
            if (string.IsNullOrWhiteSpace(sms.Body))
                continue;

            // Skip obviously non-transactional provider SMS (OTPs, standing-order confirmations,
            // marketing) before touching the parser/LLM: a real debit/credit always carries a
            // currency token or a decimal amount. This keeps a manual sync fast and avoids wasting
            // (cloud) LLM calls on messages that can never become a ledger row.
            if (!LooksTransactional(sms.Body))
            {
                skipped++;
                continue;
            }

            ct.ThrowIfCancellationRequested();
            try
            {
                // Prepend the sender so the pipeline can attribute the bank/wallet even when the
                // SMS body does not repeat the provider name (the sender IS the bank, e.g. "D360
                // Bank"). This mirrors how the message appears on the phone and becomes part of the
                // stored provenance.
                var text = $"{sms.Sender}\n{sms.Body}";
                await parser.HandleAsync(new ParseTransactionCommand(text), ct);
                imported++;
            }
            catch (DuplicateTransactionException)
            {
                duplicates++;
            }
            catch (Exception ex)
            {
                failed++;
                logger.LogWarning(ex, "Could not import SMS from {Sender} dated {SentAt:u}.", sms.Sender, sms.SentAtUtc);
            }
        }

        var result = new SmsIngestionResult(messages.Count, imported, duplicates, failed);
        logger.LogInformation(
            "SMS ingestion {From:yyyy-MM-dd}..{To:yyyy-MM-dd}: scanned {Scanned}, imported {Imported}, duplicates {Duplicates}, failed {Failed}, skipped {Skipped} (non-transactional).",
            fromUtc, toUtc, result.Scanned, result.Imported, result.Duplicates, result.Failed, skipped);
        return result;
    }

    // A number adjacent to a currency token (either order, e.g. "175SR" or "SAR 78.10") or a
    // decimal amount (e.g. 251.00) — present in every real transaction SMS, absent from OTPs,
    // standing-order confirmations and marketing. Note banks often omit the space ("175SR"), so a
    // \b-anchored currency match is not enough.
    private const string Currency = @"SAR|SR|ريال|ر\.?\s?س|USD|AED|درهم|دولار";
    private static readonly Regex TransactionalHint = new(
        $@"\d\s*(?:{Currency})|(?:{Currency})\s*\d|\d+\.\d{{2}}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool LooksTransactional(string body) => TransactionalHint.IsMatch(body);

    /// <summary>Convenience overload: ingest the calendar month that contains <paramref name="anyDayInMonth"/>.</summary>
    public Task<SmsIngestionResult> HandleCurrentMonthAsync(DateOnly anyDayInMonth, CancellationToken ct)
    {
        var from = new DateTime(anyDayInMonth.Year, anyDayInMonth.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);
        return HandleAsync(from, to, ct);
    }
}
