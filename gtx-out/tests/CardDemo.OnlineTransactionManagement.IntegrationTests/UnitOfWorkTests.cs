using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class UnitOfWorkTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public UnitOfWorkTests(TestDbFixture fixture)
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
    public async Task Rollback_RevertsAllStagedChanges()
    {
        await using var db = _fixture.CreateContext();
        var accountRepo = new AccountRepository(db);
        var txnRepo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        var txnId = _accountId + 200_000_000;

        await uow.BeginAsync(CancellationToken.None);

        // Stage a transaction insert
        txnRepo.StageAdd(new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "03",
            CategoryCode = 1,
            Source = "TEST",
            Description = "ROLLBACK TEST",
            Amount = 100m,
            OriginDate = new DateTime(2024, 1, 1),
            ProcessDate = new DateTime(2024, 1, 1)
        });

        // Stage an account update
        var account = await accountRepo.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(account);
        account.Balance = 999999m;
        accountRepo.StageUpdate(account);

        // Rollback instead of commit
        await uow.RollbackAsync(CancellationToken.None);

        // Verify nothing was persisted
        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var txn = await db2.Transactions.FirstOrDefaultAsync(t => t.Id == paddedId);
        Assert.Null(txn);

        var accountRepo2 = new AccountRepository(db2);
        var reloaded = await accountRepo2.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(reloaded);
        Assert.Equal(500m, reloaded.Balance);
    }
}
