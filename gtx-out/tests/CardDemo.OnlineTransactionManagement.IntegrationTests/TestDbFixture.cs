using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Data;
using CardDemo.OnlineTransactionManagement.Domain;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.IntegrationTests;

public sealed class TestDbFixture : IAsyncLifetime
{
    public string ConnectionString { get; }

    public TestDbFixture()
    {
        ConnectionString = Environment.GetEnvironmentVariable("CARDDEMO_TEST_DB")
            ?? throw new InvalidOperationException(
                "CARDDEMO_TEST_DB environment variable is not set. " +
                "Integration tests require a real PostgreSQL connection string.");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    public OnlineTransactionManagementDbContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<OnlineTransactionManagementDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new OnlineTransactionManagementDbContext(opts);
    }

    /// <summary>
    /// Inserts a full parent chain: account -> card_cross_reference.
    /// Uses raw SQL to avoid EF tracking issues across tests.
    /// </summary>
    public async Task SeedAccountAndCardAsync(
        long accountId, string cardNumber, decimal balance,
        decimal cycleCredit = 0m, decimal cycleDebit = 0m,
        char activeStatus = 'Y', int customerId = 1)
    {
        await using var db = CreateContext();
        // Upsert account
        await db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO accounts (id, active_status, current_balance, credit_limit, cash_credit_limit,
                open_date, expiration_date, reissue_date, current_cycle_credit, current_cycle_debit,
                address_zip, group_id)
              VALUES ({0}, {1}, {2}, 10000.00, 5000.00,
                '2020-01-01', '2030-12-31', '2025-01-01', {3}, {4},
                '12345', 'GRP1')
              ON CONFLICT (id) DO UPDATE SET
                active_status = EXCLUDED.active_status,
                current_balance = EXCLUDED.current_balance,
                current_cycle_credit = EXCLUDED.current_cycle_credit,
                current_cycle_debit = EXCLUDED.current_cycle_debit",
            accountId, activeStatus.ToString(), balance, cycleCredit, cycleDebit);

        // Upsert card
        await db.Database.ExecuteSqlRawAsync(
            @"INSERT INTO card_cross_references (card_number, customer_id, account_id)
              VALUES ({0}, {1}, {2})
              ON CONFLICT (card_number) DO NOTHING",
            cardNumber, customerId, accountId);
    }

    /// <summary>
    /// Cleans up test data by deleting children first, then parents.
    /// </summary>
    public async Task CleanupAsync(long accountId, string cardNumber)
    {
        await using var db = CreateContext();
        // Delete transactions referencing this card first
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM transactions WHERE card_number = {0}", cardNumber);
        // Delete card
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM card_cross_references WHERE card_number = {0}", cardNumber);
        // Delete account
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM accounts WHERE id = {0}", accountId);
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<TestDbFixture> { }
