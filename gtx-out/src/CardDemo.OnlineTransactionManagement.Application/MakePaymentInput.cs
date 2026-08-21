namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Input record for making an online bill payment.</summary>
public sealed record MakePaymentInput
{
    /// <summary>Account identifier. Required.</summary>
    public string? AccountId { get; init; }

    /// <summary>Confirmation flag. Must be "Y" or "y" to proceed.</summary>
    public string? Confirmation { get; init; }
}