namespace Masroof.Domain.Entities;

/// <summary>
/// A record of one parse attempt: model, prompt version, hints, raw output and latency.
/// Together with <see cref="Feedback"/> this becomes a labelled dataset for evals / fine-tuning.
/// </summary>
public class ParseTrace
{
    public long TraceId { get; set; }
    public long? TransactionId { get; set; }
    public string Model { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public string? HintsJson { get; set; }
    public string? RawOutput { get; set; }
    public int LatencyMs { get; set; }
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }

    public Transaction? Transaction { get; set; }
}
