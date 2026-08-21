using CardDemo.OnlineTransactionManagement.Application;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class InMemoryCardRepository : ICardRepository
{
    private readonly List<CardRecord> _cards = new();

    public void Seed(CardRecord card)
    {
        _cards.Add(card);
    }

    public Task<CardRecord?> GetByCardNumberAsync(string cardNumber, CancellationToken ct)
    {
        var card = _cards.FirstOrDefault(c => c.CardNumber == cardNumber);
        return Task.FromResult(card);
    }

    public Task<CardRecord?> GetByAccountIdAsync(long accountId, CancellationToken ct)
    {
        var card = _cards.FirstOrDefault(c => c.AccountId == accountId && c.ActiveStatus == 'Y');
        return Task.FromResult(card);
    }
}