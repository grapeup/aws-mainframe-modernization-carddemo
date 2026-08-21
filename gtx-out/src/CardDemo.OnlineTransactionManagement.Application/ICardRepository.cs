namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Repository for card cross-reference records.</summary>
public interface ICardRepository
{
    /// <summary>Retrieves a card record by its 16-character card number. Returns null if not found.</summary>
    Task<CardRecord?> GetByCardNumberAsync(string cardNumber, CancellationToken ct);

    /// <summary>Retrieves the first active card for a given account. Returns null if none found.</summary>
    Task<CardRecord?> GetByAccountIdAsync(long accountId, CancellationToken ct);
}