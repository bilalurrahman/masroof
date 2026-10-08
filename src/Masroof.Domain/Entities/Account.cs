namespace Masroof.Domain.Entities;

/// <summary>A bank card or wallet the user transacts from. Optional per transaction.</summary>
public class Account
{
    public int AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? BankCode { get; set; }   // RAJHI, SNB, STCPAY ...
    public string? Last4 { get; set; }
    public string? Nickname { get; set; }

    /// <summary>Last 4 digits of the IBAN, when known — transfer SMS often name the
    /// destination by IBAN tail rather than card tail.</summary>
    public string? IbanTail { get; set; }

    /// <summary>True when this is one of the user's own accounts. A transfer whose other
    /// side is an own account is internal movement (<c>transfer_internal</c>), not spending.</summary>
    public bool IsOwn { get; set; }

    public User? User { get; set; }
}
