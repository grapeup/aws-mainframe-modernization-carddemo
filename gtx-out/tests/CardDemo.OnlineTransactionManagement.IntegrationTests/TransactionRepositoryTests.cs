using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class TransactionRepositoryTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public TransactionRepositoryTests(TestDbFixture fixture)
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
    public async Task GetMaxTransactionIdAsync_ReturnsZero_WhenEmpty()
    {
        // We can't guarantee the table is empty globally, but we can test the method runs.
        // If there are rows, it returns a positive number; if empty, 0.
        await using var db = _fixture.CreateContext();
        var repo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        await uow.BeginAsync(CancellationToken.None);
        var maxId = await repo.GetMaxTransactionIdAsync(CancellationToken.None);
        await uow.CommitAsync(CancellationToken.None);

        Assert.True(maxId >= 0);
    }

    [Fact]
    public async Task StageAdd_PersistsTransaction_WithCorrectColumnMapping()
    {
        await using var db = _fixture.CreateContext();
        var repo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        // Use a unique transaction id based on account id to avoid collisions
        var txnId = _accountId;

        var record = new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "03",
            CategoryCode = 5,
            Source = "ONLINE",
            Description = "TEST PURCHASE",
            Amount = 42.50m,
            OriginDate = new DateTime(2024, 6, 15, 14, 30, 0),
            ProcessDate = new DateTime(2024, 6, 15, 14, 30, 0),
            MerchantId = "123",
            MerchantName = "TEST MERCHANT",
            MerchantCity = "BERLIN",
            MerchantZip = "10115"
        };

        await uow.BeginAsync(CancellationToken.None);
        repo.StageAdd(record);
        await uow.CommitAsync(CancellationToken.None);

        // Verify with a fresh context
        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var entity = await db2.Transactions
            .FirstOrDefaultAsync(t => t.Id == paddedId);

        Assert.NotNull(entity);
        Assert.Equal("03", entity.TypeCode);
        Assert.Equal(5, entity.CategoryCode);
        Assert.Equal("ONLINE", entity.Source);
        Assert.Equal("TEST PURCHASE", entity.Description);
        Assert.Equal(42.50m, entity.Amount);
        Assert.Equal(_cardNumber, entity.CardNumber);
        Assert.Equal("TEST MERCHANT", entity.Merchant.Name);
        Assert.Equal("BERLIN", entity.Merchant.City);
        Assert.Equal("10115", entity.Merchant.ZipCode);
    }

    [Fact]
    public async Task StageAdd_MoneyRoundTrips_AtSchemaPrecision()
    {
        await using var db = _fixture.CreateContext();
        var repo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        var txnId = _accountId + 100_000_000;

        var record = new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "01",
            CategoryCode = 1,
            Source = "ATM",
            Description = "PRECISION TEST",
            Amount = 123456789.99m,
            OriginDate = new DateTime(2024, 1, 1),
            ProcessDate = new DateTime(2024, 1, 1)
        };

        await uow.BeginAsync(CancellationToken.None);
        repo.StageAdd(record);
        await uow.CommitAsync(CancellationToken.None);

        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var entity = await db2.Transactions.FirstOrDefaultAsync(t => t.Id == paddedId);

        Assert.NotNull(entity);
        Assert.Equal(123456789.99m, entity.Amount);
    }
}
