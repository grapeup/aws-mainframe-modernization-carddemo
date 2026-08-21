using System.Threading;
using System.Threading.Tasks;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class CardRepositoryTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public CardRepositoryTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId = UniqueIds.NextAccountId();
        _cardNumber = UniqueIds.CardNumberFor(_accountId);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId, _cardNumber, 500m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId, _cardNumber);
    }

    [Fact]
    public async Task GetByCardNumberAsync_ReturnsCard()
    {
        await using var db = _fixture.CreateContext();
        var repo = new CardRepository(db);

        var card = await repo.GetByCardNumberAsync(_cardNumber, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(_cardNumber, card.CardNumber);
        Assert.Equal(_accountId, card.AccountId);
        Assert.Equal('Y', card.ActiveStatus);
    }

    [Fact]
    public async Task GetByCardNumberAsync_ReturnsNull_WhenNotFound()
    {
        await using var db = _fixture.CreateContext();
        var repo = new CardRepository(db);

        var card = await repo.GetByCardNumberAsync("9999999999999999", CancellationToken.None);

        Assert.Null(card);
    }

    [Fact]
    public async Task GetByAccountIdAsync_ReturnsCard()
    {
        await using var db = _fixture.CreateContext();
        var repo = new CardRepository(db);

        var card = await repo.GetByAccountIdAsync(_accountId, CancellationToken.None);

        Assert.NotNull(card);
        Assert.Equal(_cardNumber, card.CardNumber);
        Assert.Equal(_accountId, card.AccountId);
    }

    [Fact]
    public async Task GetByAccountIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var db = _fixture.CreateContext();
        var repo = new CardRepository(db);

        var card = await repo.GetByAccountIdAsync(-999, CancellationToken.None);

        Assert.Null(card);
    }
}
