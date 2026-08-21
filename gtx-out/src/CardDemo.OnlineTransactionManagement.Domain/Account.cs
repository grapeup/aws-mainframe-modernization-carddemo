#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

/// <summary>A customer credit-card account with its balance, credit limits, and billing-cycle accumulators</summary>
public class Account
{
    /// <summary>fits in bigint</summary>
    public long Id { get; set; }

    /// <summary>Single character flag; see open_questions for possible enum</summary>
    public string ActiveStatus { get; set; } = string.Empty;

    /// <summary>Signed monetary value — decimal, never double</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Monetary value</summary>
    public decimal CreditLimit { get; set; }

    /// <summary>Monetary value</summary>
    public decimal CashCreditLimit { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd</summary>
    public DateOnly OpenDate { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd; legacy typo 'EXPIRAION' corrected</summary>
    public DateOnly ExpirationDate { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd</summary>
    public DateOnly ReissueDate { get; set; }

    /// <summary>Billing-cycle credit accumulator</summary>
    public decimal CurrentCycleCredit { get; set; }

    /// <summary>Billing-cycle debit accumulator</summary>
    public decimal CurrentCycleDebit { get; set; }

    /// <summary>Alphanumeric zip/postal code</summary>
    public string AddressZip { get; set; } = string.Empty;

    /// <summary>Account grouping identifier</summary>
    public string GroupId { get; set; } = string.Empty;
}