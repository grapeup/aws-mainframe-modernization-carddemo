using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class MakePaymentIntegrationTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public MakePaymentIntegrationTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId = UniqueIds.NextAccountId();
        _cardNumber = UniqueIds.CardNumberFor(_accountId);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId, _cardNumber, 750.50m, 0m, 0m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId, _cardNumber);
    }

    private TransactionService CreateService(
        CardDemo.OnlineTransactionManagement.Data.OnlineTransactionManagementDbContext db)
    {
        var ar = new AccountRepository(db);
        var cr = new CardRepository(db);
        var tr = new TransactionRepository(db);
        var uow = new UnitOfWork(db);
        return new TransactionService(ar, cr, tr, uow);
    }

    [Fact]
    public async Task MakePayment_PaysFullBalance_SetsBalanceToZero()
    {
        await using var db = _fixture.CreateContext();
        var svc = CreateService(db);

        var result = await svc.MakePaymentAsync(new MakePaymentInput
        {
            AccountId = _accountId.ToString(),
            Confirmation = "Y"
        }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(750.50m, result.PaymentAmount);
        Assert.True(result.TransactionId > 0);

        // Verify account balance is zero
        await using var db2 = _fixture.CreateContext();
        var repo2 = new AccountRepository(db2);
        var acct = await repo2.GetByIdAsync(_accountId, CancellationToken.None);
        Assert.NotNull(acct);
        Assert.Equal(0m, acct.Balance);
        Assert.Equal(750.50m, acct.CurrentCycleCredit);
    }

    [Fact]
    public async Task MakePayment_ZeroBalance_ReturnsNothingToPay()
    {
        var zeroAcctId = UniqueIds.NextAccountId();
        var zeroCard = UniqueIds.CardNumberFor(zeroAcctId);
        await _fixture.SeedAccountAndCardAsync(zeroAcctId, zeroCard, 0m);

        try
        {
            await using var db = _fixture.CreateContext();
            var svc = CreateService(db);

            var result = await svc.MakePaymentAsync(new MakePaymentInput
            {
                AccountId = zeroAcctId.ToString(),
                Confirmation = "Y"
            }, CancellationToken.None);

            Assert.False(result.Success);
            Assert.Equal(TransactionError.NothingToPay, result.Error);
        }
        finally
        {
            await _fixture.CleanupAsync(zeroAcctId, zeroCard);
        }
    }

    [Fact]
    public async Task MakePayment_NoConfirmation_ReturnsPaymentNotConfirmed()
    {
        await using var db = _fixture.CreateContext();
        var svc = CreateService(db);

        var result = await svc.MakePaymentAsync(new MakePaymentInput
        {
            AccountId = _accountId.ToString(),
            Confirmation = "N"
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.PaymentNotConfirmed, result.Error);
    }

    [Fact]
    public async Task MakePayment_MissingAccountId_ReturnsAccountIdMissing()
    {
        await using var db = _fixture.CreateContext();
        var svc = CreateService(db);

        var result = await svc.MakePaymentAsync(new MakePaymentInput
        {
            AccountId = null,
            Confirmation = "Y"
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.AccountIdMissing, result.Error);
    }

    [Fact]
    public async Task MakePayment_NonexistentAccount_ReturnsAccountNotFound()
    {
        await using var db = _fixture.CreateContext();
        var svc = CreateService(db);

        var result = await svc.MakePaymentAsync(new MakePaymentInput
        {
            AccountId = "-1",
            Confirmation = "Y"
        }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.AccountNotFound, result.Error);
    }
}
