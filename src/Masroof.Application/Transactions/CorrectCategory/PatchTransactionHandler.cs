using System.Text.Json;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Masroof.Domain.Enums;
using Masroof.Domain.Normalization;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Transactions.CorrectCategory;

/// <summary>
/// Applies a correction and, when the category changes, upserts a per-user MerchantRule and
/// queues an embedding job — so the same subject is auto-categorized next time, no retraining.
/// </summary>
public sealed class PatchTransactionHandler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<TransactionDto> HandleAsync(PatchTransactionCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        var txn = await db.Transactions
            .FirstOrDefaultAsync(t => t.TransactionId == cmd.Id && !t.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Transaction), cmd.Id);

        var now = clock.UtcNow;
        var feedback = new List<Feedback>();
        var changed = false;

        // --- Category (drives the learn loop) ---
        if (cmd.CategoryCode is not null)
        {
            var newCategory = await db.Categories.FirstOrDefaultAsync(c => c.Code == cmd.CategoryCode, ct)
                              ?? throw new NotFoundException(nameof(Category), cmd.CategoryCode);

            if (newCategory.CategoryId != txn.CategoryId)
            {
                var oldCode = await db.Categories
                    .Where(c => c.CategoryId == txn.CategoryId)
                    .Select(c => c.Code)
                    .FirstOrDefaultAsync(ct);

                feedback.Add(NewFeedback(txn.TransactionId, "category", oldCode, newCategory.Code, now));
                txn.CategoryId = newCategory.CategoryId;
                changed = true;

                await LearnRuleAsync(userId, txn, newCategory, now, ct);
            }
        }

        // --- Amount ---
        if (cmd.Amount is { } amount && amount != txn.Amount)
        {
            feedback.Add(NewFeedback(txn.TransactionId, "amount", txn.Amount.ToString("0.##"), amount.ToString("0.##"), now));
            txn.Amount = amount;
            changed = true;
        }

        // --- Date ---
        if (cmd.TxnDate is { } date && date != txn.TxnDate)
        {
            feedback.Add(NewFeedback(txn.TransactionId, "date", txn.TxnDate.ToString("yyyy-MM-dd"), date.ToString("yyyy-MM-dd"), now));
            txn.TxnDate = date;
            changed = true;
        }

        // --- Counterparty ---
        if (cmd.Counterparty is not null && cmd.Counterparty.Trim() != (txn.Counterparty ?? string.Empty))
        {
            var newCp = cmd.Counterparty.Trim();
            feedback.Add(NewFeedback(txn.TransactionId, "counterparty", txn.Counterparty, newCp, now));
            txn.Counterparty = newCp.Length == 0 ? null : newCp;
            txn.CounterpartyNorm = txn.Counterparty is null ? null : TextNormalizer.NormalizeSubject(txn.Counterparty);
            changed = true;
        }

        // --- Direction ---
        if (cmd.Direction is not null)
        {
            var newDir = TransactionDirectionExtensions.ParseDirection(cmd.Direction);
            if (newDir != txn.Direction)
            {
                feedback.Add(NewFeedback(txn.TransactionId, "direction", txn.Direction.ToDbValue(), newDir.ToDbValue(), now));
                txn.Direction = newDir;
                changed = true;
            }
        }

        if (changed)
        {
            txn.IsCorrected = true;
            txn.Source = TransactionSource.Manual;
            db.Feedback.AddRange(feedback);
            await db.SaveChangesAsync(ct);
        }

        var category = await db.Categories.FirstAsync(c => c.CategoryId == txn.CategoryId, ct);
        var name = currentUser.Locale.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? category.NameAr : category.NameEn;
        return new TransactionDto(
            txn.TransactionId,
            txn.Direction.ToDbValue(),
            txn.Amount,
            txn.Currency,
            txn.Counterparty,
            txn.Channel,
            txn.TxnDate,
            new CategoryRef(category.Code, name),
            txn.Confidence,
            txn.Source.ToDbValue(),
            txn.IsCorrected,
            txn.CreatedAt);
    }

    private async Task LearnRuleAsync(Guid userId, Transaction txn, Category newCategory, DateTime now, CancellationToken ct)
    {
        var subject = txn.CounterpartyNorm
                      ?? (txn.Counterparty is not null ? TextNormalizer.NormalizeSubject(txn.Counterparty) : null);
        if (string.IsNullOrWhiteSpace(subject))
            return; // nothing stable to learn from

        var ruleText = $"Treat {subject} as {newCategory.Code}.";

        var rule = await db.MerchantRules
            .FirstOrDefaultAsync(r => r.UserId == userId && r.SubjectNorm == subject, ct);

        if (rule is null)
        {
            rule = new MerchantRule
            {
                UserId = userId,
                SubjectNorm = subject,
                CategoryId = newCategory.CategoryId,
                RuleText = ruleText,
                HitCount = 0,
                UpdatedAt = now
            };
            db.MerchantRules.Add(rule);
        }
        else
        {
            rule.CategoryId = newCategory.CategoryId;
            rule.RuleText = ruleText;
            rule.EmbeddingModel = null; // stale; the embed job will refresh it
            rule.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct); // assign RuleId before enqueuing

        db.Outbox.Add(new OutboxMessage
        {
            Type = OutboxTypes.EmbedRule,
            PayloadJson = JsonSerializer.Serialize(new { ruleId = rule.RuleId }),
            CreatedAt = now
        });
    }

    private static Feedback NewFeedback(long txnId, string field, string? oldVal, string? newVal, DateTime now) =>
        new() { TransactionId = txnId, Field = field, OldValue = oldVal, NewValue = newVal, CreatedAt = now };
}
