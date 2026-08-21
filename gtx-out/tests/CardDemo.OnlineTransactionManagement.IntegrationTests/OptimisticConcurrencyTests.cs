using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Data;
using CardDemo.OnlineTransactionManagement.Infrastructure;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

[Collection("Database")]
public sealed class OptimisticConcurrencyTests : IAsyncLifetime
{
    private readonly TestDbFixture _fixture;
    private readonly long _accountId;
    private readonly string _cardNumber;

    public OptimisticConcurrencyTests(TestDbFixture fixture)
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
    public async Task CompetingAccountUpdates_SecondIsRejected_WithDbUpdateConcurrencyException()
    {
        // First context reads the account
        await using var db1 = CreateContextWithXmin();
        var repo1 = new AccountRepository(db1);
        var uow1 = new UnitOfWork(db1);

        // We need to read via tracking for xmin to work
        var entity1 = await db1.Accounts.FirstAsync(a => a.Id == _accountId);

        // Second context reads the same account
        await using var db2 = CreateContextWithXmin();
        var entity2 = await db2.Accounts.FirstAsync(a => a.Id == _accountId);

        // First writer updates and commits
        entity1.CurrentBalance = 400m;
        await db1.SaveChangesAsync();

        // Second writer tries to update the same row — should fail
        entity2.CurrentBalance = 300m;
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
            () => db2.SaveChangesAsync());
    }

    private OnlineTransactionManagementDbContext CreateContextWithXmin()
    {
        var opts = new DbContextOptionsBuilder<OnlineTransactionManagementDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;
        var ctx = new XminDbContext(opts);
        return ctx;
    }
}

/// <summary>
/// Derived context that adds xmin concurrency token without declaring a property.
/// </summary>
internal class XminDbContext : OnlineTransactionManagementDbContext
{
    public XminDbContext(DbContextOptions<OnlineTransactionManagementDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<CardDemo.OnlineTransactionManagement.Domain.Account>().UseXminAsConcurrencyToken();
    }
}
