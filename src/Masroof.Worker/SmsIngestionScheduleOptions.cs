namespace Masroof.Worker;

/// <summary>Schedule and scope for the recurring bank/wallet SMS ingestion job.</summary>
public sealed class SmsIngestionScheduleOptions
{
    public const string SectionName = "SmsIngestion";

    /// <summary>Master switch for the weekly job.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Run one ingestion immediately when the worker starts, in addition to the weekly cadence.</summary>
    public bool RunOnStartup { get; set; } = true;

    /// <summary>Days between runs. 7 = weekly.</summary>
    public int IntervalDays { get; set; } = 7;

    /// <summary>The user whose ledger the imported rows belong to (defaults to the dev user).</summary>
    public Guid UserId { get; set; } = Guid.Parse("00000000-0000-0000-0000-000000000001");

    /// <summary>Locale/currency used when provisioning the user row if it does not yet exist.</summary>
    public string Locale { get; set; } = "en";
    public string Currency { get; set; } = "SAR";
}
