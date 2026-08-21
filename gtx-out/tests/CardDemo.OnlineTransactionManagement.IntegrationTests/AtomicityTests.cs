using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class AtomicityTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public AtomicityTests(TestDbFixture fixture)
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
    public async Task CommitAsync_IsAtomic_TransactionAndAccountUpdateTogether()
    {
        // Prove that both the transaction insert and account update are committed together.
        await using var db = _fixture.CreateContext();
        var accountRepo = new AccountRepository(db);
        var txnRepo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        var txnId = _accountId + 300_000_000;

        await uow.BeginAsync(CancellationToken.None);

        txnRepo.StageAdd(new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "03",
            CategoryCode = 1,
            Source = "TEST",
            Description = "ATOMICITY TEST",
            Amount = 50m,
            OriginDate = new DateTime(2024, 1, 1),
            ProcessDate = new DateTime(2024, 1, 1)
        });

        var account = await accountRepo.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(account);
        account.Balance = 550m;
        accountRepo.StageUpdate(account);

        await uow.CommitAsync(CancellationToken.None);

        // Verify both persisted
        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var txn = await db2.Transactions.FirstOrDefaultAsync(t => t.Id == paddedId);
        Assert.NotNull(txn);

        var accountRepo2 = new AccountRepository(db2);
        var reloaded = await accountRepo2.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(550m, reloaded.Balance);
    }
}
