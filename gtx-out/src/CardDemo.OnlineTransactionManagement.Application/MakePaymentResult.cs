namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Result of making an online bill payment.</summary>
public sealed record MakePaymentResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public required bool Success { get; init; }

    /// <summary>Error code if the operation failed.</summary>
    public TransactionError Error { get; init; }

    /// <summary>The generated transaction ID on success.</summary>
    public long TransactionId { get; init; }

    /// <summary>The payment amount (equal to the previous balance).</summary>
    public decimal PaymentAmount { get; init; }
}