using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class TimezoneConversionTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public TimezoneConversionTests(TestDbFixture fixture)
    {
        _fixture = fixture;
        _accountId = UniqueIds.NextAccountId();
        _cardNumber = UniqueIds.CardNumberFor(_accountId);
    }

    public async Task InitializeAsync()
    {
        await _fixture.SeedAccountAndCardAsync(_accountId, _cardNumber, 1000m);
    }

    public async Task DisposeAsync()
    {
        await _fixture.CleanupAsync(_accountId, _cardNumber);
    }

    [Fact]
    public async Task WinterTimestamp_IsStoredAsUtcMinus1Hour()
    {
        // 2024-01-15 10:00:00 in Europe/Berlin (CET = UTC+1)
        // Expected UTC: 2024-01-15 09:00:00
        var winterLocal = new DateTime(2024, 1, 15, 10, 0, 0);
        var expectedUtc = new DateTimeOffset(2024, 1, 15, 9, 0, 0, TimeSpan.Zero);

        await using var db = _fixture.CreateContext();
        var repo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        var txnId = _accountId + 400_000_000;

        await uow.BeginAsync(CancellationToken.None);
        repo.StageAdd(new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "01",
            CategoryCode = 1,
            Source = "TEST",
            Description = "WINTER TZ TEST",
            Amount = 10m,
            OriginDate = winterLocal,
            ProcessDate = winterLocal
        });
        await uow.CommitAsync(CancellationToken.None);

        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var entity = await db2.Transactions.FirstOrDefaultAsync(t => t.Id == paddedId);

        Assert.NotNull(entity);
        Assert.Equal(expectedUtc, entity.OriginatedAt);
        Assert.Equal(expectedUtc, entity.ProcessedAt);
    }

    [Fact]
    public async Task SummerTimestamp_IsStoredAsUtcMinus2Hours()
    {
        // 2024-07-15 10:00:00 in Europe/Berlin (CEST = UTC+2)
        // Expected UTC: 2024-07-15 08:00:00
        var summerLocal = new DateTime(2024, 7, 15, 10, 0, 0);
        var expectedUtc = new DateTimeOffset(2024, 7, 15, 8, 0, 0, TimeSpan.Zero);

        await using var db = _fixture.CreateContext();
        var repo = new TransactionRepository(db);
        var uow = new UnitOfWork(db);

        var txnId = _accountId + 500_000_000;

        await uow.BeginAsync(CancellationToken.None);
        repo.StageAdd(new TransactionRecord
        {
            Id = txnId,
            AccountId = _accountId,
            CardNumber = _cardNumber,
            TypeCode = "01",
            CategoryCode = 1,
            Source = "TEST",
            Description = "SUMMER TZ TEST",
            Amount = 10m,
            OriginDate = summerLocal,
            ProcessDate = summerLocal
        });
        await uow.CommitAsync(CancellationToken.None);

        await using var db2 = _fixture.CreateContext();
        var paddedId = txnId.ToString().PadLeft(16, '0');
        var entity = await db2.Transactions.FirstOrDefaultAsync(t => t.Id == paddedId);

        Assert.NotNull(entity);
        Assert.Equal(expectedUtc, entity.OriginatedAt);
        Assert.Equal(expectedUtc, entity.ProcessedAt);
    }
}
