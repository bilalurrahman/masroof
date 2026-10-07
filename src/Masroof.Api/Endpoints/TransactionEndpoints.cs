using System.Text.Json;
using FluentValidation;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Application.Transactions.CorrectCategory;
using Masroof.Application.Transactions.DeleteTransaction;
using Masroof.Application.Transactions.GetLedger;
using Masroof.Application.Transactions.ParseTransaction;
using Masroof.Domain.Entities;
using Masroof.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Masroof.Api.Endpoints;

public static class TransactionEndpoints
{
    public static void MapTransactionEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/transactions/parse", ParseAsync)
            .RequireRateLimiting("llm")
            .WithSummary("Parse one message into a saved ledger row.");

        group.MapPost("/transactions/parse-batch", ParseBatchAsync)
            .WithSummary("Queue a batch of messages (separated by a blank line) for background parsing.");

        group.MapGet("/transactions", GetLedgerAsync)
            .WithSummary("List the ledger, paged and filtered.");

        group.MapPatch("/transactions/{id:long}", PatchAsync)
            .WithSummary("Edit a transaction; a category change teaches a rule.");

        group.MapDelete("/transactions/{id:long}", DeleteAsync)
            .WithSummary("Soft-delete a transaction.");
    }

    private static async Task<IResult> ParseAsync(
        ParseRequest req,
        ParseTransactionHandler handler,
        IValidator<ParseTransactionCommand> validator,
        CancellationToken ct)
    {
        var cmd = new ParseTransactionCommand(req.Text ?? string.Empty);
        await validator.ValidateAndThrowAsync(cmd, ct);
        var result = await handler.HandleAsync(cmd, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> ParseBatchAsync(
        ParseBatchRequest req,
        MasroofDbContext db,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken ct)
    {
        var messages = (req.Text ?? string.Empty)
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(m => m.Length > 0)
            .ToList();

        if (messages.Count == 0)
            return Results.BadRequest(new ProblemDetails { Title = "No messages", Status = 400 });

        var outbox = new OutboxMessage
        {
            Type = OutboxTypes.ReparseBatch,
            PayloadJson = JsonSerializer.Serialize(new { userId = currentUser.UserId, messages }),
            CreatedAt = clock.UtcNow
        };
        db.Outbox.Add(outbox);
        await db.SaveChangesAsync(ct);

        return Results.Accepted($"/api/jobs/{outbox.OutboxId}", new JobAccepted(outbox.OutboxId, messages.Count));
    }

    private static async Task<IResult> GetLedgerAsync(
        GetLedgerHandler handler,
        CancellationToken ct,
        [FromQuery] string? month = null,
        [FromQuery] string? category = null,
        [FromQuery] string? direction = null,
        [FromQuery] int? accountId = null,
        [FromQuery] string? q = null,
        [FromQuery] bool needsReview = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        int? year = null, mon = null;
        if (MonthKey.TryParse(month, out var key)) { year = key.Year; mon = key.Month; }

        var query = new GetLedgerQuery(year, mon, category, direction, accountId, q, needsReview, page, pageSize);
        var result = await handler.HandleAsync(query, ct);
        return Results.Ok(result);
    }

    private static async Task<IResult> PatchAsync(
        long id,
        PatchTransactionRequest req,
        PatchTransactionHandler handler,
        IValidator<PatchTransactionCommand> validator,
        CancellationToken ct)
    {
        var cmd = new PatchTransactionCommand(id, req.CategoryCode, req.Amount, req.TxnDate, req.Counterparty, req.Direction);
        await validator.ValidateAndThrowAsync(cmd, ct);
        var dto = await handler.HandleAsync(cmd, ct);
        return Results.Ok(dto);
    }

    private static async Task<IResult> DeleteAsync(long id, DeleteTransactionHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(id, ct);
        return Results.NoContent();
    }
}
