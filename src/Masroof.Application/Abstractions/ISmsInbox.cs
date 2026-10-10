namespace Masroof.Application.Abstractions;

/// <summary>One SMS/text message read from the device inbox.</summary>
/// <param name="Sender">The sender identifier as stored by the OS (e.g. "SABB", "Alinma", "STCPay").</param>
/// <param name="Body">The message text.</param>
/// <param name="SentAtUtc">When the message was received, in UTC.</param>
public sealed record SmsMessage(string Sender, string Body, DateTime SentAtUtc);

/// <summary>
/// Reads bank/wallet SMS from the host device's message store. The concrete implementation is
/// platform-specific (e.g. the macOS Messages database); the Application layer only depends on
/// this abstraction so ingestion stays testable and host-agnostic.
/// </summary>
public interface ISmsInbox
{
    /// <summary>
    /// Returns messages from the configured providers that were received in the half-open window
    /// <paramref name="fromUtc"/> (inclusive) .. <paramref name="toUtc"/> (exclusive).
    /// </summary>
    Task<IReadOnlyList<SmsMessage>> ReadAsync(DateTime fromUtc, DateTime toUtc, CancellationToken ct);
}
