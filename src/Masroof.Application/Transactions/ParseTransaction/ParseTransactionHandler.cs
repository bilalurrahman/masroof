using System.Text.Json;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Masroof.Domain.Enums;
using Masroof.Domain.Normalization;
using Masroof.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Masroof.Application.Transactions.ParseTransaction;

/// <summary>
/// Orchestrates the parse pipeline: normalize → dedupe → pre-parser → rules → LLM →
/// merge + post-override → validate → persist (row + trace). Pre-parser fields are
/// authoritative where present; an exact rule always wins the category.
/// </summary>
public sealed class ParseTransactionHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPreParser preParser,
    IRuleStore ruleStore,
    ILlmTransactionParser llm,
    ILogger<ParseTransactionHandler> logger)
{
    public async Task<ParseResponse> HandleAsync(ParseTransactionCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var normalized = TextNormalizer.NormalizeInput(cmd.Text);
        if (normalized.Length == 0)
            throw new UnparseableLlmOutputException("Message was empty after normalization.");

        var hash = TextNormalizer.Sha256(normalized);

        var duplicate = await db.Transactions
            .Where(t => t.UserId == userId && t.RawTextHash == hash)
            .Select(t => (long?)t.TransactionId)
            .FirstOrDefaultAsync(ct);
        if (duplicate is { } existingId)
            throw new DuplicateTransactionException(existingId);

        var pre = preParser.TryParse(normalized);
        var counterpartyNorm = pre?.Counterparty is { } cp ? TextNormalizer.NormalizeSubject(cp) : null;

        var matches = await ruleStore.MatchAsync(userId, normalized, counterpartyNorm, ct);
        var hints = matches.Select(m => m.RuleText)
                           .Distinct(StringComparer.OrdinalIgnoreCase)
                           .Take(8)
                           .ToList();
        var exact = matches.FirstOrDefault(m => m.Tier == RuleMatchTier.Exact);

        var preConfident = pre is { Confidence: >= 1.0, Amount: > 0, Direction: not null };

        ParseResult? llmResult = null;
        ParsedTransaction? parsed = null;
        var source = TransactionSource.Llm;
        var reparsePending = false;

        if (preConfident && exact is not null)
        {
            // Fully deterministic: structure from the pre-parser, category from the exact rule.
            source = TransactionSource.PreParser;
        }
        else
        {
            try
            {
                llmResult = await llm.ParseAsync(normalized, hints, currentUser.Currency, ct);
            }
            catch (LlmUnavailableException) when (preConfident)
            {
                logger.LogWarning("LLM unavailable; saving pre-parsed row for later reparse (user {UserId}).", userId);
                source = TransactionSource.PreParser;
                reparsePending = true;
            }

            if (!reparsePending)
            {
                if (llmResult is null || !llmResult.Succeeded || llmResult.Transaction is null)
                {
                    await PersistFailedTraceAsync(llmResult, hints, ct);

                    if (preConfident)
                    {
                        logger.LogWarning("LLM output unusable; falling back to pre-parsed row (user {UserId}).", userId);
                        source = TransactionSource.PreParser;
                        reparsePending = true;
                    }
                    else
                    {
                        throw new UnparseableLlmOutputException(
                            "The model did not return a usable transaction.",
                            llmResult?.RawOutput);
                    }
                }
                else
                {
                    parsed = llmResult.Transaction;
                }
            }
        }

        // --- Merge: pre-parser fields are authoritative where present, LLM fills the gaps. ---
        var amount = pre?.Amount ?? parsed?.Amount ?? 0m;
        var direction = pre?.Direction
                        ?? (parsed is not null ? TransactionDirectionExtensions.ParseDirection(parsed.Direction) : TransactionDirection.Debit);
        var currency = (pre?.Currency ?? parsed?.Currency ?? currentUser.Currency).Trim().ToUpperInvariant();
        var counterparty = Coalesce(pre?.Counterparty, parsed?.Counterparty);
        var channel = TransactionChannel.Normalize(Coalesce(pre?.Channel, parsed?.Channel));
        var last4 = Coalesce(pre?.Last4, parsed?.AccountLast4);
        var txnDate = pre?.Date ?? parsed?.Date ?? clock.Today;

        // Category: LLM suggestion, then rule post-override (rules beat the model).
        var categoryCode = CategoryCodes.ResolveOrOther(exact?.CategoryCode ?? parsed?.Category);
        var confidence = exact is not null
            ? 1.0
            : (parsed?.Confidence ?? pre?.Confidence ?? 0.0);

        ValidateBusinessRules(amount, currency, txnDate);

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Code == categoryCode, ct)
                       ?? throw new UnparseableLlmOutputException($"Unknown category '{categoryCode}'.");

        var accountId = await ResolveAccountAsync(userId, pre?.BankCode, last4, ct);

        var now = clock.UtcNow;
        var txn = new Transaction
        {
            UserId = userId,
            AccountId = accountId,
            RawText = cmd.Text.Trim(),
            RawTextHash = hash,
            Direction = direction,
            Amount = amount,
            Currency = currency,
            Counterparty = counterparty,
            CounterpartyNorm = counterparty is null ? null : TextNormalizer.NormalizeSubject(counterparty),
            Channel = channel,
            TxnDate = txnDate,
            CategoryId = category.CategoryId,
            Confidence = Math.Round((decimal)Math.Clamp(confidence, 0.0, 1.0), 3),
            Source = source,
            IsCorrected = false,
            CreatedAt = now
        };
        db.Transactions.Add(txn);

        db.ParseTraces.Add(new ParseTrace
        {
            Transaction = txn,
            Model = llmResult?.Model ?? "preparser",
            PromptVersion = llmResult?.PromptVersion ?? "n/a",
            HintsJson = hints.Count > 0 ? JsonSerializer.Serialize(hints) : null,
            RawOutput = llmResult?.RawOutput,
            LatencyMs = llmResult?.LatencyMs ?? 0,
            Succeeded = true,
            CreatedAt = now
        });

        if (reparsePending)
        {
            db.Outbox.Add(new OutboxMessage
            {
                Type = OutboxTypes.ReparsePending,
                PayloadJson = JsonSerializer.Serialize(new { userId }),
                CreatedAt = now
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Lost a dedupe race: surface the row that won.
            var winner = await db.Transactions
                .Where(t => t.UserId == userId && t.RawTextHash == hash)
                .Select(t => (long?)t.TransactionId)
                .FirstOrDefaultAsync(ct);
            if (winner is { } winnerId && winnerId != txn.TransactionId)
                throw new DuplicateTransactionException(winnerId);
            throw;
        }

        if (exact is not null)
            await ruleStore.IncrementHitCountAsync(exact.RuleId, ct);

        var name = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? category.NameAr : category.NameEn;
        return new ParseResponse(
            txn.TransactionId,
            direction.ToDbValue(),
            txn.Amount,
            txn.Currency,
            txn.Counterparty,
            txn.Channel,
            txn.TxnDate,
            new CategoryRef(category.Code, name),
            txn.Confidence,
            source.ToDbValue(),
            hints,
            llmResult?.LatencyMs ?? 0);
    }

    private static string? Coalesce(string? a, string? b) =>
        !string.IsNullOrWhiteSpace(a) ? a!.Trim() : (!string.IsNullOrWhiteSpace(b) ? b!.Trim() : null);

    private static void ValidateBusinessRules(decimal amount, string currency, DateOnly txnDate)
    {
        if (amount <= 0)
            throw new UnparseableLlmOutputException("Amount must be greater than zero.");
        if (!Currencies.IsAllowed(currency))
            throw new UnparseableLlmOutputException($"Currency '{currency}' is not allowed.");
        if (txnDate > DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            throw new UnparseableLlmOutputException("Transaction date is too far in the future.");
    }

    private async Task<int?> ResolveAccountAsync(Guid userId, string? bankCode, string? last4, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(bankCode) && string.IsNullOrWhiteSpace(last4))
            return null;

        var account = await db.Accounts.FirstOrDefaultAsync(
            a => a.UserId == userId && a.BankCode == bankCode && a.Last4 == last4, ct);
        if (account is not null)
            return account.AccountId;

        account = new Account { UserId = userId, BankCode = bankCode, Last4 = last4 };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);
        return account.AccountId;
    }

    private async Task PersistFailedTraceAsync(ParseResult? result, IReadOnlyList<string> hints, CancellationToken ct)
    {
        db.ParseTraces.Add(new ParseTrace
        {
            Model = result?.Model ?? "unknown",
            PromptVersion = result?.PromptVersion ?? "n/a",
            HintsJson = hints.Count > 0 ? JsonSerializer.Serialize(hints) : null,
            RawOutput = result?.RawOutput,
            LatencyMs = result?.LatencyMs ?? 0,
            Succeeded = false,
            Error = result?.Error ?? "No LLM result.",
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
