namespace Masroof.Application.Abstractions;

/// <summary>Abstracts the system clock so parse-date defaults and traces are testable.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
    DateOnly Today { get; }
}
