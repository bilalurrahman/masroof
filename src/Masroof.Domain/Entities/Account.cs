namespace Masroof.Domain.Entities;

/// <summary>A bank card or wallet the user transacts from. Optional per transaction.</summary>
public class Account
{
    public int AccountId { get; set; }
    public Guid UserId { get; set; }
    public string? BankCode { get; set; }   // RAJHI, SNB, STCPAY ...
    public string? Last4 { get; set; }
    public string? Nickname { get; set; }

    public User? User { get; set; }
}
