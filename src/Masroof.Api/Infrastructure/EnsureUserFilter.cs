using Masroof.Application.Abstractions;
using Masroof.Domain.Entities;
using Masroof.Infrastructure.Persistence;

namespace Masroof.Api.Infrastructure;

/// <summary>
/// Ensures a <see cref="User"/> row exists for the authenticated caller before a handler runs,
/// so transaction/account/rule foreign keys resolve. Provisioned from JWT claims (JIT).
/// </summary>
public sealed class EnsureUserFilter(MasroofDbContext db, ICurrentUser currentUser, IClock clock) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var userId = currentUser.UserId;
        var exists = await db.Users.FindAsync([userId], ctx.HttpContext.RequestAborted) is not null;
        if (!exists)
        {
            db.Users.Add(new User
            {
                UserId = userId,
                DisplayName = currentUser.DisplayName ?? "User",
                Locale = currentUser.Locale,
                Currency = currentUser.Currency,
                CreatedAt = clock.UtcNow
            });
            await db.SaveChangesAsync(ctx.HttpContext.RequestAborted);
        }

        return await next(ctx);
    }
}
