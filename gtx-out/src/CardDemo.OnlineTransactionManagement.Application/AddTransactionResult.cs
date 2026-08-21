namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Result of adding a new transaction.</summary>
public sealed record AddTransactionResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Error code if the operation failed.</summary>
    public TransactionError Error { get; init; }

    /// <summary>The generated transaction ID on success.</summary>
    public long TransactionId { get; init; }

    /// <summary>Updated account balance after the transaction.</summary>
    public decimal UpdatedBalance { get; init; }
}