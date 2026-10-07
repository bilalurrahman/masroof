using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace Masroof.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/reports/summary", SummaryAsync)
            .WithSummary("Totals, by category, and change vs last month.");

        group.MapGet("/reports/trend", TrendAsync)
            .WithSummary("Monthly debit/credit series for charts.");
    }

    private static async Task<IResult> SummaryAsync(
        IReportQueries reports,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken ct,
        [FromQuery] string? month = null)
    {
        var key = MonthKey.TryParse(month, out var parsed) ? parsed : MonthKey.FromDate(clock.Today);
        var summary = await reports.GetMonthlySummaryAsync(currentUser.UserId, key.Year, key.Month, ct);
        return Results.Ok(summary);
    }

    private static async Task<IResult> TrendAsync(
        IReportQueries reports,
        ICurrentUser currentUser,
        CancellationToken ct,
        [FromQuery] int months = 6)
    {
        var trend = await reports.GetTrendAsync(currentUser.UserId, months, ct);
        return Results.Ok(trend);
    }
}
