using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Transactions.DeleteTransaction;

/// <summary>Soft-deletes a transaction (sets IsDeleted). The dedupe hash is retained.</summary>
public sealed class DeleteTransactionHandler(IAppDbContext db, ICurrentUser currentUser, IClock clock)
{
    public async Task HandleAsync(long id, CancellationToken ct)
    {
        var txn = await db.Transactions.FirstOrDefaultAsync(
                      t => t.TransactionId == id && t.UserId == currentUser.UserId && !t.IsDeleted, ct)
                  ?? throw new NotFoundException(nameof(Transaction), id);

        txn.IsDeleted = true;
        txn.DeletedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
