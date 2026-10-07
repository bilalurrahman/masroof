namespace Masroof.Domain.Entities;

/// <summary>
/// A reliable background job row (transactional outbox). The worker polls unprocessed
/// rows and dispatches them, e.g. "EmbedRule", "ReparseBatch".
/// </summary>
public class OutboxMessage
{
    public long OutboxId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Well-known outbox message types.</summary>
public static class OutboxTypes
{
    public const string EmbedRule = "EmbedRule";
    public const string ReparseBatch = "ReparseBatch";
    public const string ReparsePending = "ReparsePending";
}
