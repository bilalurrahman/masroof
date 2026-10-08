using FluentValidation;
using Masroof.Application.Abstractions;
using Masroof.Application.Common;
using Masroof.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Accounts;

/// <summary>A bank account/card the user transacts from, as shown on the Accounts screen.</summary>
public sealed record AccountDto(
    int AccountId,
    string? BankCode,
    string? Last4,
    string? Nickname,
    string? IbanTail,
    bool IsOwn,
    int TransactionCount);

public sealed record UpsertAccountRequest(
    string? BankCode,
    string? Last4,
    string? Nickname,
    string? IbanTail,
    bool IsOwn);

/// <summary>Lists the user's accounts (auto-discovered from SMS and any added manually).</summary>
public sealed class ListAccountsHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<AccountDto>> HandleAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var rows = await db.Accounts
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsOwn)
            .ThenBy(a => a.Nickname)
            .Select(a => new AccountDto(
                a.AccountId, a.BankCode, a.Last4, a.Nickname, a.IbanTail, a.IsOwn,
                db.Transactions.Count(t => t.AccountId == a.AccountId && !t.IsDeleted)))
            .ToListAsync(ct);
        return rows;
    }
}

/// <summary>Registers a new account the user owns (e.g. a destination account not yet seen in an SMS).</summary>
public sealed class CreateAccountHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<AccountDto> HandleAsync(UpsertAccountRequest req, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var last4 = AccountInput.Tail(req.Last4);
        var bankCode = string.IsNullOrWhiteSpace(req.BankCode) ? "GENERIC" : req.BankCode.Trim().ToUpperInvariant();
        if (last4 is null)
            throw new ValidationException("A 4-digit card/account number (last4) is required to add an account.");

        var existing = await db.Accounts.FirstOrDefaultAsync(
            a => a.UserId == userId && a.BankCode == bankCode && a.Last4 == last4, ct);
        if (existing is not null)
            throw new ValidationException($"An account {bankCode} ending {last4} already exists.");

        var account = new Account
        {
            UserId = userId,
            BankCode = bankCode,
            Last4 = last4,
            Nickname = AccountInput.Clean(req.Nickname, 60),
            IbanTail = AccountInput.Tail(req.IbanTail),
            IsOwn = req.IsOwn
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        return new AccountDto(account.AccountId, account.BankCode, account.Last4,
            account.Nickname, account.IbanTail, account.IsOwn, 0);
    }
}

/// <summary>Updates an account's nickname, IBAN tail, and whether it is one of the user's own.</summary>
public sealed class UpdateAccountHandler(IAppDbContext db, ICurrentUser currentUser)
{
    public async Task<AccountDto> HandleAsync(int accountId, UpsertAccountRequest req, CancellationToken ct)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(
                          a => a.AccountId == accountId && a.UserId == currentUser.UserId, ct)
                      ?? throw new NotFoundException(nameof(Account), accountId);

        account.Nickname = AccountInput.Clean(req.Nickname, 60);
        account.IbanTail = AccountInput.Tail(req.IbanTail);
        account.IsOwn = req.IsOwn;
        await db.SaveChangesAsync(ct);

        var count = await db.Transactions.CountAsync(t => t.AccountId == accountId && !t.IsDeleted, ct);
        return new AccountDto(account.AccountId, account.BankCode, account.Last4,
            account.Nickname, account.IbanTail, account.IsOwn, count);
    }
}

file static class AccountInput
{
    /// <summary>Keeps the last 4 digits of a card/IBAN input, or null if none.</summary>
    public static string? Tail(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var digits = new string(raw.Where(char.IsDigit).ToArray());
        return digits.Length >= 4 ? digits[^4..] : null;
    }

    public static string? Clean(string? raw, int max)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var t = raw.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
