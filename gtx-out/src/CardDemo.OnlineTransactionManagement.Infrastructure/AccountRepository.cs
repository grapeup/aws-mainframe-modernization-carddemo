using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Data;
using CardDemo.OnlineTransactionManagement.Domain;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

public sealed class AccountRepository : IAccountRepository
{
    private readonly OnlineTransactionManagementDbContext _db;

    public AccountRepository(OnlineTransactionManagementDbContext db)
    {
        _db = db;
    }

    public async Task<AccountRecord?> GetByIdAsync(long accountId, CancellationToken ct)
    {
        var entity = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == accountId, ct);

        if (entity is null)
            return null;

        return new AccountRecord
        {
            Id = entity.Id,
            ActiveStatus = string.IsNullOrEmpty(entity.ActiveStatus) ? ' ' : entity.ActiveStatus[0],
            Balance = entity.CurrentBalance,
            CurrentCycleCredit = entity.CurrentCycleCredit,
            CurrentCycleDebit = entity.CurrentCycleDebit
        };
    }

    public void StageUpdate(AccountRecord account)
    {
        var entity = _db.Accounts.Local.FindEntry(account.Id)?.Entity;
        if (entity is null)
        {
            // Attach a stub and mark only the fields we care about as modified
            entity = new Account
            {
                Id = account.Id,
                ActiveStatus = account.ActiveStatus.ToString(),
                CurrentBalance = account.Balance,
                CurrentCycleCredit = account.CurrentCycleCredit,
                CurrentCycleDebit = account.CurrentCycleDebit,
                // Required fields that won't be updated but must be set for the entity
                CreditLimit = 0m,
                CashCreditLimit = 0m,
                OpenDate = DateOnly.MinValue,
                ExpirationDate = DateOnly.MinValue,
                ReissueDate = DateOnly.MinValue,
                AddressZip = string.Empty,
                GroupId = string.Empty
            };
            _db.Accounts.Attach(entity);
        }

        entity.CurrentBalance = account.Balance;
        entity.CurrentCycleCredit = account.CurrentCycleCredit;
        entity.CurrentCycleDebit = account.CurrentCycleDebit;

        _db.Entry(entity).Property(e => e.CurrentBalance).IsModified = true;
        _db.Entry(entity).Property(e => e.CurrentCycleCredit).IsModified = true;
        _db.Entry(entity).Property(e => e.CurrentCycleDebit).IsModified = true;
    }
}
