namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Represents a transaction record.</summary>
public sealed class TransactionRecord
{
    /// <summary>Unique transaction identifier.</summary>
    public required long Id { get; set; }

    /// <summary>Account identifier. Mandatory foreign key.</summary>
    public required long AccountId { get; set; }

    /// <summary>Card number. Mandatory foreign key.</summary>
    public required string CardNumber { get; set; }

    /// <summary>Transaction type code.</summary>
    public required string TypeCode { get; set; }

    /// <summary>Transaction category code.</summary>
    public required int CategoryCode { get; set; }

    /// <summary>Transaction source.</summary>
    public required string Source { get; set; }

    /// <summary>Transaction description.</summary>
    public required string Description { get; set; }

    /// <summary>Transaction amount.</summary>
    public required decimal Amount { get; set; }

    /// <summary>Date the transaction originated.</summary>
    public required DateTime OriginDate { get; set; }

    /// <summary>Date the transaction is processed.</summary>
    public required DateTime ProcessDate { get; set; }

    /// <summary>Merchant identifier.</summary>
    public string? MerchantId { get; set; }

    /// <summary>Merchant name.</summary>
    public string? MerchantName { get; set; }

    /// <summary>Merchant city.</summary>
    public string? MerchantCity { get; set; }

    /// <summary>Merchant zip code.</summary>
    public string? MerchantZip { get; set; }
}