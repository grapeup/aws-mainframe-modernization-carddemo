using CardDemo.OnlineTransactionManagement.Application;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    private readonly InMemoryAccountRepository _accounts;
    private readonly InMemoryTransactionRepository _transactions;

    public bool WasCommitted { get; private set; }
    public bool WasBegun { get; private set; }

    public InMemoryUnitOfWork(
        InMemoryAccountRepository accounts,
        InMemoryTransactionRepository transactions)
    {
        _accounts = accounts;
        _transactions = transactions;
    }

    public Task BeginAsync(CancellationToken ct)
    {
        WasBegun = true;
        return Task.CompletedTask;
    }

    public Task CommitAsync(CancellationToken ct)
    {
        WasCommitted = true;
        _accounts.ApplyStaged();
        _transactions.ApplyStaged();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct)
    {
        _accounts.ClearStaged();
        _transactions.ClearStaged();
        return Task.CompletedTask;
    }
}