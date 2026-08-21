using System.Threading;
using System.Threading.Tasks;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class AccountRepositoryTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public AccountRepositoryTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId = UniqueIds.NextAccountId();
        _cardNumber = UniqueIds.CardNumberFor(_accountId);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId, _cardNumber, 1234.56m, 100m, 200m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId, _cardNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAccount_WithCorrectDecimalPrecision()
    {
        await using var db = _fixture.CreateContext();
        var repo = new AccountRepository(db);

        var account = await repo.GetByIdAsync(_accountId, CancellationToken.None);

        Assert.NotNull(account);
        Assert.Equal(_accountId, account.Id);
        Assert.Equal('Y', account.ActiveStatus);
        Assert.Equal(1234.56m, account.Balance);
        Assert.Equal(100m, account.CurrentCycleCredit);
        Assert.Equal(200m, account.CurrentCycleDebit);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        await using var db = _fixture.CreateContext();
        var repo = new AccountRepository(db);

        var account = await repo.GetByIdAsync(-999, CancellationToken.None);

        Assert.Null(account);
    }

    [Fact]
    public async Task StageUpdate_PersistsBalanceChanges()
    {
        await using var db = _fixture.CreateContext();
        var repo = new AccountRepository(db);
        var uow = new UnitOfWork(db);

        var account = await repo.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(account);

        account.Balance = 999.99m;
        account.CurrentCycleCredit = 50.00m;
        account.CurrentCycleDebit = 75.25m;

        await uow.BeginAsync(CancellationToken.None);
        repo.StageUpdate(account);
        await uow.CommitAsync(CancellationToken.None);

        // Re-read with a fresh context
        await using var db2 = _fixture.CreateContext();
        var repo2 = new AccountRepository(db2);
        var reloaded = await repo2.GetByIdAsync(_accountId, CancellationToken.None);

        Assert.NotNull(reloaded);
        Assert.Equal(999.99m, reloaded.Balance);
        Assert.Equal(50.00m, reloaded.CurrentCycleCredit);
        Assert.Equal(75.25m, reloaded.CurrentCycleDebit);
    }
}
