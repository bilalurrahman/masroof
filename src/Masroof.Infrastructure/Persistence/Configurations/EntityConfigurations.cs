using Masroof.Domain.Entities;
using Masroof.Domain.Enums;
using Masroof.Domain.Taxonomy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Masroof.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.UserId);
        b.Property(x => x.UserId).ValueGeneratedNever();
        b.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Locale).HasColumnType("varchar(10)").HasDefaultValue("en").IsRequired();
        b.Property(x => x.Currency).HasColumnType("char(3)").HasDefaultValue("SAR").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.HasKey(x => x.CategoryId);
        b.Property(x => x.CategoryId).ValueGeneratedNever();
        b.Property(x => x.Code).HasColumnType("varchar(40)").IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.Property(x => x.NameEn).HasMaxLength(60).IsRequired();
        b.Property(x => x.NameAr).HasMaxLength(60).IsRequired();
        b.Property(x => x.Icon).HasColumnType("varchar(40)");
        b.Property(x => x.Color).HasColumnType("char(7)");

        b.HasData(CategorySeed.Items.Select(i => new Category
        {
            CategoryId = i.CategoryId,
            Code = i.Code,
            NameEn = i.NameEn,
            NameAr = i.NameAr,
            Icon = i.Icon,
            Color = i.Color
        }));
    }
}

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> b)
    {
        b.ToTable("Accounts");
        b.HasKey(x => x.AccountId);
        b.Property(x => x.AccountId).ValueGeneratedOnAdd();
        b.Property(x => x.BankCode).HasColumnType("varchar(20)");
        b.Property(x => x.Last4).HasColumnType("char(4)");
        b.Property(x => x.Nickname).HasMaxLength(60);
        b.HasIndex(x => new { x.UserId, x.BankCode, x.Last4 }).IsUnique().HasDatabaseName("UQ_Accounts");
        b.HasOne(x => x.User).WithMany(u => u.Accounts).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("Transactions", t =>
        {
            t.HasCheckConstraint("CK_Txn_Amount", "[Amount] >= 0");
            t.HasCheckConstraint("CK_Txn_Direction", "[Direction] IN ('debit','credit')");
        });
        b.HasKey(x => x.TransactionId);
        b.Property(x => x.TransactionId).ValueGeneratedOnAdd();

        b.Property(x => x.RawText).HasMaxLength(1000).IsRequired();
        b.Property(x => x.RawTextHash).HasColumnType("binary(32)").IsRequired();

        b.Property(x => x.Direction)
            .HasColumnType("varchar(6)")
            .HasConversion(v => v.ToDbValue(), v => TransactionDirectionExtensions.ParseDirection(v))
            .IsRequired();

        b.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        b.Property(x => x.Currency).HasColumnType("char(3)").IsRequired();
        b.Property(x => x.Counterparty).HasMaxLength(200);
        b.Property(x => x.CounterpartyNorm).HasMaxLength(200);
        b.Property(x => x.Channel).HasColumnType("varchar(20)");
        b.Property(x => x.TxnDate).HasColumnType("date").IsRequired();
        b.Property(x => x.Confidence).HasColumnType("decimal(4,3)");

        b.Property(x => x.Source)
            .HasColumnType("varchar(12)")
            .HasConversion(v => v.ToDbValue(), v => TransactionSourceExtensions.ParseSource(v))
            .IsRequired();

        b.Property(x => x.IsCorrected).HasDefaultValue(false);
        b.Property(x => x.IsDeleted).HasDefaultValue(false);
        b.Property(x => x.DeletedAt).HasColumnType("datetime2(0)");
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.Property(x => x.RowVer).IsRowVersion();

        b.HasIndex(x => new { x.UserId, x.RawTextHash }).IsUnique().HasDatabaseName("UQ_Txn_Dedupe");
        b.HasIndex(x => new { x.UserId, x.TxnDate })
            .HasDatabaseName("IX_Txn_User_Date")
            .IncludeProperties(x => new { x.Amount, x.Direction, x.CategoryId });

        b.HasOne(x => x.User).WithMany(u => u.Transactions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        // ClientSetNull (DB: NO ACTION) — avoids a second cascade path into Transactions
        // (User→Accounts→Transactions alongside User→Transactions), which SQL Server rejects.
        // AccountId is optional, so EF still nulls it on tracked entities when an Account is removed.
        b.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.ClientSetNull);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MerchantRuleConfiguration : IEntityTypeConfiguration<MerchantRule>
{
    public void Configure(EntityTypeBuilder<MerchantRule> b)
    {
        b.ToTable("MerchantRules");
        b.HasKey(x => x.RuleId);
        b.Property(x => x.RuleId).ValueGeneratedOnAdd();
        b.Property(x => x.SubjectNorm).HasMaxLength(200).IsRequired();
        b.Property(x => x.RuleText).HasMaxLength(400).IsRequired();
        b.Property(x => x.EmbeddingModel).HasColumnType("varchar(80)");
        b.Property(x => x.HitCount).HasDefaultValue(0);
        b.Property(x => x.UpdatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");

        // The VECTOR(1024) column is created in the migration and managed by the rules engine (Dapper).
        b.Ignore(x => x.Embedding);

        b.HasIndex(x => new { x.UserId, x.SubjectNorm }).IsUnique().HasDatabaseName("UQ_Rule");
        b.HasOne(x => x.User).WithMany(u => u.Rules).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ParseTraceConfiguration : IEntityTypeConfiguration<ParseTrace>
{
    public void Configure(EntityTypeBuilder<ParseTrace> b)
    {
        b.ToTable("ParseTraces");
        b.HasKey(x => x.TraceId);
        b.Property(x => x.TraceId).ValueGeneratedOnAdd();
        b.Property(x => x.Model).HasColumnType("varchar(80)").IsRequired();
        b.Property(x => x.PromptVersion).HasColumnType("varchar(20)").IsRequired();
        b.Property(x => x.HintsJson).HasColumnType("nvarchar(max)");
        b.Property(x => x.RawOutput).HasColumnType("nvarchar(max)");
        b.Property(x => x.Error).HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasOne(x => x.Transaction).WithMany(t => t.Traces).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.SetNull);
    }
}

public sealed class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> b)
    {
        b.ToTable("Feedback");
        b.HasKey(x => x.FeedbackId);
        b.Property(x => x.FeedbackId).ValueGeneratedOnAdd();
        b.Property(x => x.Field).HasColumnType("varchar(30)").IsRequired();
        b.Property(x => x.OldValue).HasMaxLength(200);
        b.Property(x => x.NewValue).HasMaxLength(200);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasOne(x => x.Transaction).WithMany(t => t.Feedback).HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("Outbox");
        b.HasKey(x => x.OutboxId);
        b.Property(x => x.OutboxId).ValueGeneratedOnAdd();
        b.Property(x => x.Type).HasColumnType("varchar(50)").IsRequired();
        b.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        b.Property(x => x.ProcessedAt).HasColumnType("datetime2(0)");
        b.Property(x => x.Attempts).HasDefaultValue(0);
        b.Property(x => x.LastError).HasMaxLength(1000);
        b.Property(x => x.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
        b.HasIndex(x => x.ProcessedAt).HasDatabaseName("IX_Outbox_Unprocessed");
    }
}
