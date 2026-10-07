namespace Masroof.Application.Abstractions;

/// <summary>
/// The authenticated caller, resolved from the JWT. Every data access and every Ask tool
/// scopes to <see cref="UserId"/> — it is never taken from request bodies or LLM arguments.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The Keycloak subject (sub) claim. Throws if unauthenticated.</summary>
    Guid UserId { get; }

    string? DisplayName { get; }
    string Locale { get; }
    string Currency { get; }
}
