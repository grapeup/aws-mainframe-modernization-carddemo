using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class AddTransactionIntegrationTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public AddTransactionIntegrationTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId = UniqueIds.NextAccountId();
        _cardNumber = UniqueIds.CardNumberFor(_accountId);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId, _cardNumber, 1000m, 0m, 0m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId, _cardNumber);
    }

    private (AccountRepository, CardRepository, TransactionRepository, UnitOfWork, TransactionService) CreateService(
        CardDemo.OnlineTransactionManagement.Data.OnlineTransactionManagementDbContext db)
    {
        var ar = new AccountRepository(db);
        var cr = new CardRepository(db);
        var tr = new TransactionRepository(db);
        var uow = new UnitOfWork(db);
        var svc = new TransactionService(ar, cr, tr, uow);
        return (ar, cr, tr, uow, svc);
    }

    [Fact]
    public async Task AddTransaction_Purchase_IncreasesBalanceAndDebit()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, _, _, svc) = CreateService(db);

        var result = await svc.AddTransactionAsync(new AddTransactionInput
        {
            AccountId = _accountId,
            TypeCode = "03",
            CategoryCode = 5,
            Source = "ONLINE",
            Description = "INTEGRATION PURCHASE",
            Amount = "250.00",
            OriginDate = new DateTime(2024, 3, 15),
            ProcessDate = new DateTime(2024, 3, 15)
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(1250m, result.UpdatedBalance);

        // Verify account in DB
        await using var db2 = _fixture.CreateContext();
        var repo2 = new AccountRepository(db2);
        var acct = await repo2.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(acct);
        Assert.Equal(1250m, acct.Balance);
        Assert.Equal(250m, acct.CurrentCycleDebit);
    }

    [Fact]
    public async Task AddTransaction_Credit_DecreasesBalanceAndIncreasesCredit()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, _, _, svc) = CreateService(db);

        var result = await svc.AddTransactionAsync(new AddTransactionInput
        {
            AccountId = _accountId,
            TypeCode = "01",
            CategoryCode = 1,
            Source = "ONLINE",
            Description = "INTEGRATION CREDIT",
            Amount = "100.00",
            OriginDate = new DateTime(2024, 3, 15),
            ProcessDate = new DateTime(2024, 3, 15)
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(900m, result.UpdatedBalance);

        await using var db2 = _fixture.CreateContext();
        var repo2 = new AccountRepository(db2);
        var acct = await repo2.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(acct);
        Assert.Equal(900m, acct.Balance);
        Assert.Equal(100m, acct.CurrentCycleCredit);
    }

    [Fact]
    public async Task AddTransaction_ByCardNumber_Succeeds()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, _, _, svc) = CreateService(db);

        var result = await svc.AddTransactionAsync(new AddTransactionInput
        {
            CardNumber = _cardNumber,
            TypeCode = "03",
            CategoryCode = 5,
            Source = "POS",
            Description = "CARD LOOKUP TEST",
            Amount = "50.00",
            OriginDate = new DateTime(2024, 3, 15),
            ProcessDate = new DateTime(2024, 3, 15)
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.TransactionId > 0);
    }

    [Fact]
    public async Task AddTransaction_InactiveAccount_Fails()
    {
        var inactiveAcctId = UniqueIds.NextAccountId();
        var inactiveCard = UniqueIds.CardNumberFor(inactiveAcctId);
        await _fixture.SeedAccountAndCardAsync(inactiveAcctId, inactiveCard, 500m, activeStatus: 'N');

        try
        {
            await using var db = _fixture.CreateContext();
            var (_, _, _, _, svc) = CreateService(db);

            var result = await svc.AddTransactionAsync(new AddTransactionInput
            {
                AccountId = inactiveAcctId,
                TypeCode = "03",
                CategoryCode = 5,
                Source = "TEST",
                Description = "INACTIVE TEST",
                Amount = "10.00",
                OriginDate = new DateTime(2024, 3, 15),
                ProcessDate = new DateTime(2024, 3, 15)
            }, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(TransactionError.AccountNotActive, result.Error);
        }
        finally
        {
            await _fixture.CleanupAsync(inactiveAcctId, inactiveCard);
        }
    }

    [Fact]
    public async Task AddTransaction_InvalidAmount_Fails()
    {
        await using var db = _fixture.CreateContext();
        var (_, _, _, _, svc) = CreateService(db);

        var result = await svc.AddTransactionAsync(new AddTransactionInput
        {
            AccountId = _accountId,
            TypeCode = "03",
            CategoryCode = 5,
            Source = "TEST",
            Description = "BAD AMOUNT",
            Amount = "abc",
            OriginDate = new DateTime(2024, 3, 15),
            ProcessDate = new DateTime(2024, 3, 15)
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.InvalidAmountFormat, result.Error);
    }
}
