using CardDemo.OnlineTransactionManagement.Application;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class InMemoryTransactionRepository : ITransactionRepository
{
    private readonly List<TransactionRecord> _transactions = new();
    private readonly List<TransactionRecord> _stagedAdds = new();

    public void Seed(TransactionRecord txn)
    {
        _transactions.Add(txn);
    }

    public Task<long> GetMaxTransactionIdAsync(CancellationToken ct)
    {
        var max = _transactions.Count > 0 ? _transactions.Max(t => t.Id) : 0L;
        var stagedMax = _stagedAdds.Count > 0 ? _stagedAdds.Max(t => t.Id) : 0L;
        return Task.FromResult(Math.Max(max, stagedMax));
    }

    public void StageAdd(TransactionRecord transaction)
    {
        _stagedAdds.Add(transaction);
    }

    public void ApplyStaged()
    {
        _transactions.AddRange(_stagedAdds);
        _stagedAdds.Clear();
    }

    public void ClearStaged()
    {
        _stagedAdds.Clear();
    }

    public IReadOnlyList<TransactionRecord> StagedAdds => _stagedAdds;
    public IReadOnlyList<TransactionRecord> AllCommitted => _transactions;
}