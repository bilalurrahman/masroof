using System.Security.Claims;
using Masroof.Application.Abstractions;

namespace Masroof.Api.Identity;

/// <summary>Resolves <see cref="ICurrentUser"/> from the JWT on the current request.</summary>
public sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid UserId
    {
        get
        {
            var sub = Principal?.FindFirstValue("sub")
                      ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("No authenticated user on the request.");
        }
    }

    public string? DisplayName =>
        Principal?.FindFirstValue("name")
        ?? Principal?.FindFirstValue("preferred_username")
        ?? Principal?.FindFirstValue(ClaimTypes.Name);

    public string Locale => Principal?.FindFirstValue("locale") is { Length: > 0 } l ? l : "en";

    public string Currency => Principal?.FindFirstValue("currency") is { Length: 3 } c ? c.ToUpperInvariant() : "SAR";
}
