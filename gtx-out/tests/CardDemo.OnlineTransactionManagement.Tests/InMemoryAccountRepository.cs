using CardDemo.OnlineTransactionManagement.Application;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class InMemoryAccountRepository : IAccountRepository
{
    private readonly Dictionary<long, AccountRecord> _accounts = new();
    private readonly List<AccountRecord> _stagedUpdates = new();

    public void Seed(AccountRecord account)
    {
        _accounts[account.Id] = account;
    }

    public Task<AccountRecord?> GetByIdAsync(long accountId, CancellationToken ct)
    {
        _accounts.TryGetValue(accountId, out var account);
        return Task.FromResult(account);
    }

    public void StageUpdate(AccountRecord account)
    {
        _stagedUpdates.Add(account);
    }

    public void ApplyStaged()
    {
        foreach (var a in _stagedUpdates)
        {
            _accounts[a.Id] = a;
        }
        _stagedUpdates.Clear();
    }

    public void ClearStaged()
    {
        _stagedUpdates.Clear();
    }

    public AccountRecord? GetCommitted(long id)
    {
        _accounts.TryGetValue(id, out var a);
        return a;
    }

    public IReadOnlyList<AccountRecord> StagedUpdates => _stagedUpdates;
}