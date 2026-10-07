namespace Masroof.Domain.Entities;

/// <summary>A user correction to a parsed field. The raw signal behind the learn loop.</summary>
public class Feedback
{
    public long FeedbackId { get; set; }
    public long TransactionId { get; set; }
    public string Field { get; set; } = string.Empty;   // 'category', 'amount', ...
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }

    public Transaction? Transaction { get; set; }
}
