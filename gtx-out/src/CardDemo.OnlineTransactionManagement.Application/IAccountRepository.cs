namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Repository for account records.</summary>
public interface IAccountRepository
{
    /// <summary>Retrieves an account by its identifier. Returns null if not found.</summary>
    Task<AccountRecord?> GetByIdAsync(long accountId, CancellationToken ct);

    /// <summary>Stages an account update to be committed via the unit of work.</summary>
    void StageUpdate(AccountRecord account);
}