using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using CardDemo.OnlineTransactionManagement.Application;
using CardDemo.OnlineTransactionManagement.Data;

namespace CardDemo.OnlineTransactionManagement.Infrastructure;

public sealed class CardRepository : ICardRepository
{
    private readonly OnlineTransactionManagementDbContext _db;

    public CardRepository(OnlineTransactionManagementDbContext db)
    {
        _db = db;
    }

    public async Task<CardRecord?> GetByCardNumberAsync(string cardNumber, CancellationToken ct)
    {
        var entity = await _db.CardCrossReferences
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber, ct);

        if (entity is null)
            return null;

        // CardCrossReference has no ActiveStatus column; default to 'Y'
        return new CardRecord
        {
            CardNumber = entity.CardNumber,
            AccountId = entity.AccountId,
            ActiveStatus = 'Y'
        };
    }

    public async Task<CardRecord?> GetByAccountIdAsync(long accountId, CancellationToken ct)
    {
        var entity = await _db.CardCrossReferences
            .AsNoTracking()
            .Where(c => c.AccountId == accountId)
            .FirstOrDefaultAsync(ct);

        if (entity is null)
            return null;

        return new CardRecord
        {
            CardNumber = entity.CardNumber,
            AccountId = entity.AccountId,
            ActiveStatus = 'Y'
        };
    }
}
