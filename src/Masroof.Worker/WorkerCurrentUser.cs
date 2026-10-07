using Masroof.Application.Abstractions;

namespace Masroof.Worker;

/// <summary>
/// A settable <see cref="ICurrentUser"/> for background work. The outbox processor sets
/// <see cref="UserId"/> (and locale/currency) per message so user-scoped handlers and the EF
/// global query filter behave exactly as they do under an HTTP request.
/// </summary>
public sealed class WorkerCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.Empty;
    public string? DisplayName { get; set; }
    public string Locale { get; set; } = "en";
    public string Currency { get; set; } = "SAR";

    public bool IsAuthenticated => UserId != Guid.Empty;

    public void SetUser(Guid userId, string locale, string currency)
    {
        UserId = userId;
        Locale = locale;
        Currency = currency;
    }
}
