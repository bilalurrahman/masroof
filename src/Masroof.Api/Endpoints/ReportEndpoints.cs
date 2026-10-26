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

        group.MapGet("/reports/by-bank", ByBankAsync)
            .WithSummary("Spent/received per bank (D360, STC, SABB, Alinma, tiqmo) for monitoring.");
    }

    private static async Task<IResult> ByBankAsync(
        IReportQueries reports,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken ct,
        [FromQuery] string? month = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        DateOnly f, t;
        if (from is not null || to is not null)
        {
            f = from ?? to!.Value;
            t = to ?? from!.Value;
        }
        else
        {
            var key = MonthKey.TryParse(month, out var parsed) ? parsed : MonthKey.FromDate(clock.Today);
            f = new DateOnly(key.Year, key.Month, 1);
            t = f.AddMonths(1).AddDays(-1);
        }

        var banks = await reports.GetSpendByBankAsync(currentUser.UserId, f, t, ct);
        return Results.Ok(banks);
    }

    private static async Task<IResult> SummaryAsync(
        IReportQueries reports,
        ICurrentUser currentUser,
        IClock clock,
        CancellationToken ct,
        [FromQuery] string? month = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null)
    {
        // A From/To range wins over month. If only one bound is given, the other defaults sensibly.
        if (from is not null || to is not null)
        {
            var f = from ?? to!.Value;
            var t = to ?? from!.Value;
            var rangeSummary = await reports.GetRangeSummaryAsync(currentUser.UserId, f, t, ct);
            return Results.Ok(rangeSummary);
        }

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
