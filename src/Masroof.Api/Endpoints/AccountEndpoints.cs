using Masroof.Application.Accounts;
using Microsoft.AspNetCore.Mvc;

namespace Masroof.Api.Endpoints;

public static class AccountEndpoints
{
    public static void MapAccountEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/accounts", ListAsync)
            .WithSummary("List the user's accounts (auto-discovered + manually added).");

        group.MapPost("/accounts", CreateAsync)
            .WithSummary("Add an account the user owns.");

        group.MapPatch("/accounts/{id:int}", UpdateAsync)
            .WithSummary("Update an account's nickname, IBAN tail, and whether it is the user's own.");
    }

    private static async Task<IResult> ListAsync(ListAccountsHandler handler, CancellationToken ct)
    {
        var accounts = await handler.HandleAsync(ct);
        return Results.Ok(accounts);
    }

    private static async Task<IResult> CreateAsync(
        [FromBody] UpsertAccountRequest req, CreateAccountHandler handler, CancellationToken ct)
    {
        var account = await handler.HandleAsync(req, ct);
        return Results.Created($"/api/accounts/{account.AccountId}", account);
    }

    private static async Task<IResult> UpdateAsync(
        int id, [FromBody] UpsertAccountRequest req, UpdateAccountHandler handler, CancellationToken ct)
    {
        var account = await handler.HandleAsync(id, req, ct);
        return Results.Ok(account);
    }
}
