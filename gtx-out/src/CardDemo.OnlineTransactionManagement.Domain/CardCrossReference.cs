#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

/// <summary>Links a payment card number to its owning account and customer</summary>
public class CardCrossReference
{
    /// <summary>natural key</summary>
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>references a customer record outside this feature scope</summary>
    public int CustomerId { get; set; }

    /// <summary>FK to accounts.id</summary>
    public long AccountId { get; set; }
}