namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Unit of work that wraps a transaction boundary.
/// BeginAsync must be called before any serialised read;
/// CommitAsync applies all staged changes atomically.</summary>
public interface IUnitOfWork
{
    /// <summary>Opens the transaction. Any read that must be serialised with a
    /// later write has to happen inside it, so the service calls this first.</summary>
    Task BeginAsync(CancellationToken ct);

    /// <summary>Commits all staged changes atomically.</summary>
    Task CommitAsync(CancellationToken ct);

    /// <summary>Rolls back any staged changes if commit has not been called.</summary>
    Task RollbackAsync(CancellationToken ct);
}