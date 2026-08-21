using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Data;
using CardDemo.OnlineTransactionManagement.Domain;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

public sealed class TransactionRepository : ITransactionRepository
{
    private readonly OnlineTransactionManagementDbContext _db;

    public TransactionRepository(OnlineTransactionManagementDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Returns the highest existing transaction ID (parsed as long), or 0 if none exist.
    /// Uses SELECT ... ORDER BY id DESC LIMIT 1 FOR UPDATE to hold a row-level lock
    /// within the current transaction until commit.
    /// </summary>
    public async Task<long> GetMaxTransactionIdAsync(CancellationToken ct)
    {
        // The schema stores id as varchar(16). We use FOR UPDATE to lock the row.
        // If the table is empty, we return 0.
        var result = await _db.Database
            .SqlQueryRaw<string>(
                "SELECT id AS \"Value\" FROM transactions ORDER BY id DESC LIMIT 1 FOR UPDATE")
            .FirstOrDefaultAsync(ct);

        if (result is null)
            return 0;

        return long.TryParse(result, out var id) ? id : 0;
    }

    public void StageAdd(TransactionRecord transaction)
    {
        var entity = new Transaction
        {
            Id = transaction.Id.ToString().PadLeft(16, '0'),
            TypeCode = transaction.TypeCode,
            CategoryCode = transaction.CategoryCode,
            Source = transaction.Source,
            Description = transaction.Description,
            Amount = transaction.Amount,
            CardNumber = transaction.CardNumber,
            OriginatedAt = new DateTimeOffset(TimezoneConverter.ToUtc(transaction.OriginDate), TimeSpan.Zero),
            ProcessedAt = new DateTimeOffset(TimezoneConverter.ToUtc(transaction.ProcessDate), TimeSpan.Zero),
            Merchant = new MerchantInfo
            {
                Id = string.IsNullOrEmpty(transaction.MerchantId) ? 0 : (int.TryParse(transaction.MerchantId, out var mid) ? mid : 0),
                Name = transaction.MerchantName ?? string.Empty,
                City = transaction.MerchantCity ?? string.Empty,
                ZipCode = transaction.MerchantZip ?? string.Empty
            }
        };

        _db.Transactions.Add(entity);
    }
}
