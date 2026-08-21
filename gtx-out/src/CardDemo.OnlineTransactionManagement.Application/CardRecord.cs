namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Represents a card cross-reference record.</summary>
public sealed class CardRecord
{
    /// <summary>16-character card number.</summary>
    public required string CardNumber { get; set; }

    /// <summary>Account identifier this card is linked to.</summary>
    public required long AccountId { get; set; }

    /// <summary>Card active status. 'Y' means active.</summary>
    public required char ActiveStatus { get; set; }
}