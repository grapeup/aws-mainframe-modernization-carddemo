using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class ConcurrentIdTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId1;
    private readonly string _cardNumber1;
    private readonly long _accountId2;
    private readonly string _cardNumber2;

    public ConcurrentIdTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId1 = UniqueIds.NextAccountId();
        _cardNumber1 = UniqueIds.CardNumberFor(_accountId1);
        _accountId2 = UniqueIds.NextAccountId();
        _cardNumber2 = UniqueIds.CardNumberFor(_accountId2);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId1, _cardNumber1, 1000m);
        await _fixture.SeedAccountAndCardAsync(_accountId2, _cardNumber2, 1000m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId1, _cardNumber1);
        await _fixture.CleanupAsync(_accountId2, _cardNumber2);
    }

    [Fact]
    public async Task ConcurrentAddTransactions_ProduceUniqueIds()
    {
        const int attempts = 5;
        var ids = new ConcurrentBag<long>();

        // Run two concurrent add-transaction operations with retry on contention
        var tasks = new[]
        {
            RunWithRetry(attempts, _accountId1, _cardNumber1, ids),
            RunWithRetry(attempts, _accountId2, _cardNumber2, ids)
        };

        await Task.WhenAll(tasks);

        // Both should have produced results
        Assert.Equal(2, ids.Count);
        // IDs must be unique
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    private async Task RunWithRetry(int maxAttempts, long accountId, string cardNumber, ConcurrentBag<long> ids)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await using var db = _fixture.CreateContext();
                var accountRepo = new AccountRepository(db);
                var cardRepo = new CardRepository(db);
                var txnRepo = new TransactionRepository(db);
                var uow = new UnitOfWork(db);
                var svc = new TransactionService(accountRepo, cardRepo, txnRepo, uow);

                var result = await svc.AddTransactionAsync(new AddTransactionInput
                {
                    AccountId = accountId,
                    TypeCode = "03",
                    CategoryCode = 5,
                    Source = "ONLINE",
                    Description = "CONCURRENT TEST",
                    Amount = "10.00",
                    OriginDate = new DateTime(2024, 6, 15),
                    ProcessDate = new DateTime(2024, 6, 15)
                }, CancellationToken.None);

                Assert.True(result.Success);
                ids.Add(result.TransactionId);
                return;
            }
            catch (Exception ex) when (
                ex.InnerException?.Message?.Contains("40001") == true ||
                ex.InnerException?.Message?.Contains("could not serialize") == true ||
                ex.Message.Contains("40001") ||
                ex.Message.Contains("could not serialize") ||
                ex.Message.Contains("deadlock"))
            {
                if (attempt == maxAttempts - 1)
                    throw;
                await Task.Delay(50 * (attempt + 1));
            }
        }
    }
}
