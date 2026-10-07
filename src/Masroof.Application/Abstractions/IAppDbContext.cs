using Masroof.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Application.Abstractions;

/// <summary>
/// The write/read surface the application uses against the database. Implemented by the
/// EF Core <c>MasroofDbContext</c> in Infrastructure, which applies a global query filter
/// scoping rows to the current user. Heavy reporting aggregates go through
/// <see cref="IReportQueries"/> (Dapper) instead.
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<MerchantRule> MerchantRules { get; }
    DbSet<ParseTrace> ParseTraces { get; }
    DbSet<Feedback> Feedback { get; }
    DbSet<OutboxMessage> Outbox { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
