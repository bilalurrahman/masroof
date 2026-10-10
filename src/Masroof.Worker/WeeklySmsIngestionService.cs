using Masroof.Application.Abstractions;
using Masroof.Application.Sms;
using Masroof.Domain.Entities;
using Masroof.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Masroof.Worker;

/// <summary>
/// Runs weekly, reading bank/wallet SMS (SABB, Alinma, D360, STC Pay, tiqmo) for the current
/// calendar month from the device inbox and importing them into the user's ledger via the normal
/// parse pipeline. Scoped to the current month only; dedupe makes repeated runs idempotent, so a
/// mid-month run and the next week's run simply top up the same month without creating duplicates.
/// </summary>
public sealed class WeeklySmsIngestionService(
    IServiceScopeFactory scopeFactory,
    IOptions<SmsIngestionScheduleOptions> options,
    ILogger<WeeklySmsIngestionService> logger) : BackgroundService
{
    private readonly SmsIngestionScheduleOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Weekly SMS ingestion is disabled.");
            return;
        }

        logger.LogInformation(
            "Weekly SMS ingestion started (every {Days}d, user {UserId}).", _options.IntervalDays, _options.UserId);

        if (_options.RunOnStartup)
            await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromDays(Math.Max(1, _options.IntervalDays)));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;

            var clock = sp.GetRequiredService<IClock>();
            await EnsureUserAsync(sp, clock, ct);

            var worker = sp.GetRequiredService<WorkerCurrentUser>();
            worker.SetUser(_options.UserId, _options.Locale, _options.Currency);

            var handler = sp.GetRequiredService<IngestSmsHandler>();
            await handler.HandleCurrentMonthAsync(clock.Today, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weekly SMS ingestion run failed.");
        }
    }

    /// <summary>Provision the target user row if absent, so ledger foreign keys resolve.</summary>
    private async Task EnsureUserAsync(IServiceProvider sp, IClock clock, CancellationToken ct)
    {
        var db = sp.GetRequiredService<MasroofDbContext>();
        if (await db.Users.FindAsync([_options.UserId], ct) is not null)
            return;

        db.Users.Add(new User
        {
            UserId = _options.UserId,
            DisplayName = "SMS Import",
            Locale = _options.Locale,
            Currency = _options.Currency,
            CreatedAt = clock.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
