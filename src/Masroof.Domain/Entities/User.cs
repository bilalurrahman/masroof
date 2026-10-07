namespace Masroof.Domain.Entities;

/// <summary>An application user. <see cref="UserId"/> equals the Keycloak subject (sub) claim.</summary>
public class User
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Locale { get; set; } = "en";   // 'en' | 'ar'
    public string Currency { get; set; } = "SAR";
    public DateTime CreatedAt { get; set; }

    public ICollection<Account> Accounts { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<MerchantRule> Rules { get; set; } = [];
}
