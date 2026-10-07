using Masroof.Application.Ask;

namespace Masroof.Api.Endpoints;

public static class AskEndpoints
{
    public static void MapAskEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/ask", AskAsync)
            .RequireRateLimiting("llm")
            .WithSummary("Answer a natural-language question over the user's ledger via safe tools.");
    }

    private static async Task<IResult> AskAsync(AskHttpRequest req, IAskService ask, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Question))
            return Results.BadRequest(new { title = "Question is required." });

        var response = await ask.AskAsync(new AskRequest(req.Question.Trim()), ct);
        return Results.Ok(response);
    }
}
