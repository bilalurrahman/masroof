using Masroof.Application.Rules;
using Microsoft.AspNetCore.Mvc;

namespace Masroof.Api.Endpoints;

public static class RuleEndpoints
{
    public static void MapRuleEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/rules", ListAsync)
            .WithSummary("List what the system has learned for the user.");

        group.MapDelete("/rules/{id:int}", DeleteAsync)
            .WithSummary("Forget a learned rule.");
    }

    private static async Task<IResult> ListAsync(ListRulesHandler handler, CancellationToken ct, [FromQuery] string? q = null)
    {
        var rules = await handler.HandleAsync(q, ct);
        return Results.Ok(rules);
    }

    private static async Task<IResult> DeleteAsync(int id, DeleteRuleHandler handler, CancellationToken ct)
    {
        await handler.HandleAsync(id, ct);
        return Results.NoContent();
    }
}
