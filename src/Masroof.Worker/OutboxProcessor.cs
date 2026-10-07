using System.Text.Json;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Application.Transactions.ParseTransaction;
using Masroof.Domain.Entities;
using Masroof.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Worker;

/// <summary>
/// Polls the transactional outbox and dispatches jobs: embedding freshly learned rules,
/// parsing queued batches, and re-parsing rows that were saved while the LLM was down.
/// Each message runs in its own DI scope with the owning user set on <see cref="WorkerCurrentUser"/>.
/// </summary>
public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger)
    : BackgroundService
{
    private const int MaxAttempts = 5;
    private const int BatchSize = 20;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    // Payloads are written by the API with camelCase property names (anonymous objects),
    // so deserialize case-insensitively — otherwise fields bind to their defaults
    // (e.g. ruleId → 0, userId → Guid.Empty) and jobs silently no-op.
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox processor started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var ids = await ReadPendingIdsAsync(stoppingToken);
                foreach (var id in ids)
                    await ProcessOneAsync(id, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox polling loop error.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
        logger.LogInformation("Outbox processor stopping.");
    }

    private async Task<IReadOnlyList<long>> ReadPendingIdsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MasroofDbContext>();
        return await db.Outbox
            .Where(o => o.ProcessedAt == null && o.Attempts < MaxAttempts)
            .OrderBy(o => o.OutboxId)
            .Select(o => o.OutboxId)
            .Take(BatchSize)
            .ToListAsync(ct);
    }

    private async Task ProcessOneAsync(long outboxId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<MasroofDbContext>();
        var clock = sp.GetRequiredService<IClock>();

        var message = await db.Outbox.FirstOrDefaultAsync(o => o.OutboxId == outboxId, ct);
        if (message is null || message.ProcessedAt is not null)
            return;

        try
        {
            switch (message.Type)
            {
                case OutboxTypes.EmbedRule:
                    await HandleEmbedRuleAsync(sp, message.PayloadJson, ct);
                    break;
                case OutboxTypes.ReparseBatch:
                    await HandleReparseBatchAsync(sp, message.PayloadJson, ct);
                    break;
                case OutboxTypes.ReparsePending:
                    await HandleReparsePendingAsync(sp, message.PayloadJson, ct);
                    break;
                default:
                    logger.LogWarning("Unknown outbox type {Type} (id {Id}); marking processed.", message.Type, outboxId);
                    break;
            }

            message.ProcessedAt = clock.UtcNow;
            message.LastError = null;
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            message.Attempts += 1;
            message.LastError = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            await db.SaveChangesAsync(ct);
            logger.LogWarning(ex, "Outbox message {Id} ({Type}) failed (attempt {Attempt}).",
                outboxId, message.Type, message.Attempts);
        }
    }

    private static async Task HandleEmbedRuleAsync(IServiceProvider sp, string payloadJson, CancellationToken ct)
    {
        var ruleId = JsonSerializer.Deserialize<EmbedRulePayload>(payloadJson, JsonOpts)?.RuleId
                     ?? throw new InvalidOperationException("EmbedRule payload missing ruleId.");

        var db = sp.GetRequiredService<MasroofDbContext>();
        var embeddings = sp.GetRequiredService<IEmbeddingService>();
        var ruleStore = sp.GetRequiredService<IRuleStore>();

        var rule = await db.MerchantRules.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RuleId == ruleId, ct);
        if (rule is null)
            return; // rule was deleted before embedding; nothing to do

        var vector = await embeddings.EmbedAsync(rule.RuleText, ct);
        await ruleStore.SetEmbeddingAsync(ruleId, vector, ct);
    }

    private async Task HandleReparseBatchAsync(IServiceProvider sp, string payloadJson, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<ReparseBatchPayload>(payloadJson, JsonOpts)
                      ?? throw new InvalidOperationException("ReparseBatch payload invalid.");

        await SetUserAsync(sp, payload.UserId, ct);
        var handler = sp.GetRequiredService<ParseTransactionHandler>();

        foreach (var text in payload.Messages ?? [])
        {
            if (string.IsNullOrWhiteSpace(text))
                continue;
            try
            {
                await handler.HandleAsync(new ParseTransactionCommand(text), ct);
            }
            catch (DuplicateTransactionException)
            {
                // already captured — skip
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Batch message failed to parse for user {UserId}.", payload.UserId);
            }
        }
    }

    private async Task HandleReparsePendingAsync(IServiceProvider sp, string payloadJson, CancellationToken ct)
    {
        var userId = JsonSerializer.Deserialize<ReparsePendingPayload>(payloadJson, JsonOpts)?.UserId
                     ?? throw new InvalidOperationException("ReparsePending payload missing userId.");

        await SetUserAsync(sp, userId, ct);
        var db = sp.GetRequiredService<MasroofDbContext>();
        var ruleStore = sp.GetRequiredService<IRuleStore>();

        // Re-categorize pre-parsed rows whose category is still the catch-all, using learned rules.
        var rows = await db.Transactions
            .Where(t => t.Source == Domain.Enums.TransactionSource.PreParser
                        && t.Category!.Code == Domain.Taxonomy.CategoryCodes.Other
                        && !t.IsDeleted)
            .OrderByDescending(t => t.TransactionId)
            .Take(100)
            .ToListAsync(ct);

        if (rows.Count == 0)
            return;

        foreach (var txn in rows)
        {
            var matches = await ruleStore.MatchAsync(userId, txn.RawText, txn.CounterpartyNorm, ct);
            var best = matches.FirstOrDefault();
            if (best is null)
                continue;

            var categoryId = best.CategoryId;
            if (categoryId != txn.CategoryId)
            {
                txn.CategoryId = categoryId;
                txn.Confidence = best.Tier == RuleMatchTier.Exact ? 1.0m : (decimal)Math.Round(best.Score, 3);
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SetUserAsync(IServiceProvider sp, Guid userId, CancellationToken ct)
    {
        var worker = sp.GetRequiredService<WorkerCurrentUser>();
        var db = sp.GetRequiredService<MasroofDbContext>();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, ct);
        worker.SetUser(userId, user?.Locale ?? "en", user?.Currency ?? "SAR");
    }

    private sealed record EmbedRulePayload(int RuleId);
    private sealed record ReparseBatchPayload(Guid UserId, string[]? Messages);
    private sealed record ReparsePendingPayload(Guid UserId);
}
