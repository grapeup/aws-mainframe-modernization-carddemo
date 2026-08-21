namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Input record for adding a new transaction.</summary>
public sealed record AddTransactionInput
{
    /// <summary>Account identifier. Either this or CardNumber must be provided.</summary>
    public long? AccountId { get; init; }

    /// <summary>16-character card number. Either this or AccountId must be provided.</summary>
    public string? CardNumber { get; init; }

    /// <summary>Transaction type code (e.g. "01" credit, "02" payment, "03" purchase).</summary>
    public required string TypeCode { get; init; }

    /// <summary>Transaction category code.</summary>
    public required int CategoryCode { get; init; }

    /// <summary>Transaction source.</summary>
    public required string Source { get; init; }

    /// <summary>Transaction description.</summary>
    public required string Description { get; init; }

    /// <summary>Transaction amount as a string; validated for numeric with up to 2 decimal places.</summary>
    public required string Amount { get; init; }

    /// <summary>Date the transaction originated.</summary>
    public required DateTime OriginDate { get; init; }

    /// <summary>Date the transaction is processed.</summary>
    public required DateTime ProcessDate { get; init; }

    /// <summary>Merchant identifier.</summary>
    public string? MerchantId { get; init; }

    /// <summary>Merchant name.</summary>
    public string? MerchantName { get; init; }

    /// <summary>Merchant city.</summary>
    public string? MerchantCity { get; init; }

    /// <summary>Merchant zip code.</summary>
    public string? MerchantZip { get; init; }
}