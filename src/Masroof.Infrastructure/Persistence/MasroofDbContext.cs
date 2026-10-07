using Masroof.Application.Abstractions;
using Masroof.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Masroof.Infrastructure.Persistence;

/// <summary>
/// The EF Core context. Applies a global query filter scoping owned rows to the current user
/// (defense in depth alongside SQL Row-Level Security). The embedding VECTOR column is managed
/// outside EF by the rules engine.
/// </summary>
public sealed class MasroofDbContext(DbContextOptions<MasroofDbContext> options, ICurrentUser currentUser)
    : DbContext(options), IAppDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<MerchantRule> MerchantRules => Set<MerchantRule>();
    public DbSet<ParseTrace> ParseTraces => Set<ParseTrace>();
    public DbSet<Feedback> Feedback => Set<Feedback>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasroofDbContext).Assembly);

        // Row ownership — evaluated per query against the authenticated caller.
        modelBuilder.Entity<Transaction>().HasQueryFilter(t => t.UserId == currentUser.UserId);
        modelBuilder.Entity<Account>().HasQueryFilter(a => a.UserId == currentUser.UserId);
        modelBuilder.Entity<MerchantRule>().HasQueryFilter(r => r.UserId == currentUser.UserId);

        base.OnModelCreating(modelBuilder);
    }
}
