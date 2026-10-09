using Masroof.Application.Abstractions;
using Masroof.Domain.Enums;
using Masroof.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Transactions;

/// <summary>
/// Pairs the two legs of an internal transfer: the debit out of one own account and the credit
/// into another. After a row is saved or corrected, this matches it to an unpaired opposite-leg
/// internal transfer of the same amount within a few days and gives both a shared
/// <see cref="Domain.Entities.Transaction.TransferGroupId"/>. If a row stops being an internal
/// transfer, any pairing it was part of is dissolved. Best-effort and idempotent.
/// </summary>
public sealed class TransferPairingService(IAppDbContext db)
{
    private const int WindowDays = 3;

    public async Task SyncAsync(long transactionId, CancellationToken ct)
    {
        var txn = await db.Transactions.FirstOrDefaultAsync(t => t.TransactionId == transactionId, ct);
        if (txn is null || txn.IsDeleted)
            return;

        var internalCatId = await db.Categories
            .Where(c => c.Code == CategoryCodes.TransferInternal)
            .Select(c => (short?)c.CategoryId)
            .FirstOrDefaultAsync(ct);

        var isInternal = internalCatId is { } icid && txn.CategoryId == icid;

        if (!isInternal)
        {
            // No longer an internal transfer — dissolve any pairing it belonged to.
            if (txn.TransferGroupId is { } gid)
            {
                var legs = await db.Transactions.Where(t => t.TransferGroupId == gid).ToListAsync(ct);
                foreach (var l in legs)
                    l.TransferGroupId = null;
                await db.SaveChangesAsync(ct);
            }
            return;
        }

        if (txn.TransferGroupId is not null)
            return; // already paired

        var oppositeDir = txn.Direction == TransactionDirection.Debit
            ? TransactionDirection.Credit
            : TransactionDirection.Debit;
        var lo = txn.TxnDate.AddDays(-WindowDays);
        var hi = txn.TxnDate.AddDays(WindowDays);

        var counterpart = await db.Transactions
            .Where(t => t.TransactionId != txn.TransactionId
                        && !t.IsDeleted
                        && t.TransferGroupId == null
                        && t.CategoryId == internalCatId
                        && t.Direction == oppositeDir
                        && t.Amount == txn.Amount
                        && t.Currency == txn.Currency
                        && t.TxnDate >= lo && t.TxnDate <= hi)
            .OrderBy(t => t.TxnDate)
            .ThenBy(t => t.TransactionId)
            .FirstOrDefaultAsync(ct);

        if (counterpart is null)
            return;

        var groupId = Guid.NewGuid();
        txn.TransferGroupId = groupId;
        counterpart.TransferGroupId = groupId;
        await db.SaveChangesAsync(ct);
    }
}
