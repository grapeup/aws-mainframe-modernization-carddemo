namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Repository for transaction records.</summary>
public interface ITransactionRepository
{
    /// <summary>Returns the highest existing transaction ID, or 0 if none exist.
    /// Must be called inside an active unit-of-work transaction so the read lock
    /// is held until commit.</summary>
    Task<long> GetMaxTransactionIdAsync(CancellationToken ct);

    /// <summary>Stages a new transaction record to be committed via the unit of work.</summary>
    void StageAdd(TransactionRecord transaction);
}