using Masroof.Domain.Enums;

namespace Masroof.Domain.Entities;

/// <summary>One ledger row extracted from a bank SMS, wallet notification, or free-text note.</summary>
public class Transaction
{
    public long TransactionId { get; set; }
    public Guid UserId { get; set; }
    public int? AccountId { get; set; }

    /// <summary>The original message. Consider Always Encrypted at rest.</summary>
    public string RawText { get; set; } = string.Empty;

    /// <summary>SHA-256 of the normalized <see cref="RawText"/> for dedupe (unique per user).</summary>
    public byte[] RawTextHash { get; set; } = [];

    public TransactionDirection Direction { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "SAR";
    public string? Counterparty { get; set; }
    public string? CounterpartyNorm { get; set; }
    public string? Channel { get; set; }
    public DateOnly TxnDate { get; set; }
    public short CategoryId { get; set; }
    public decimal? Confidence { get; set; }
    public TransactionSource Source { get; set; }
    public bool IsCorrected { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When two legs of the same internal transfer (the debit out of one own account and the
    /// credit into another) are matched, they share this id. The debit leg is treated as the
    /// primary; the ledger collapses the pair to a single "A → B" row.
    /// </summary>
    public Guid? TransferGroupId { get; set; }

    /// <summary>SQL rowversion for optimistic concurrency.</summary>
    public byte[]? RowVer { get; set; }

    public User? User { get; set; }
    public Account? Account { get; set; }
    public Category? Category { get; set; }

    public ICollection<ParseTrace> Traces { get; set; } = [];
    public ICollection<Feedback> Feedback { get; set; } = [];
}
