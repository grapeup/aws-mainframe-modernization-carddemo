#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

/// <summary>Links a card number to its owning account and customer, enabling card-to-account lookups</summary>
public class CardCrossReference
{
    public string CardNumber { get; set; } = string.Empty;

    /// <summary>references a customer record (customer entity not in scope of this feature)</summary>
    public int CustomerId { get; set; }

    /// <summary>Each card is issued against exactly one account; the cross-reference owns the FK</summary>
    public long AccountId { get; set; }
}