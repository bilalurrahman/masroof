using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Masroof.Api.Identity;

public sealed class DevAuthOptions : AuthenticationSchemeOptions
{
    public const string Scheme = "Dev";
    public string UserId { get; set; } = "00000000-0000-0000-0000-000000000001";
    public string UserName { get; set; } = "Dev User";
    public string Locale { get; set; } = "en";
    public string Currency { get; set; } = "SAR";
}

/// <summary>
/// Local-only authentication that signs every request in as a fixed dev user. Enabled only when
/// <c>Auth:DevBypass</c> is true; never enable in any shared or production environment.
/// </summary>
public sealed class DevAuthenticationHandler(
    IOptionsMonitor<DevAuthOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<DevAuthOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var o = Options;
        var claims = new[]
        {
            new Claim("sub", o.UserId),
            new Claim("name", o.UserName),
            new Claim("locale", o.Locale),
            new Claim("currency", o.Currency)
        };
        var identity = new ClaimsIdentity(claims, DevAuthOptions.Scheme);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), DevAuthOptions.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
