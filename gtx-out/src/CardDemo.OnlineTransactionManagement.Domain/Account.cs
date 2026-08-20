#nullable enable

namespace CardDemo.OnlineTransactionManagement.Domain;

/// <summary>A customer credit-card account with balance, credit limits, and cycle-to-date totals</summary>
public class Account
{
    /// <summary>11-digit numeric identifier</summary>
    public long Id { get; set; }

    /// <summary>Single-character status flag; checked for 'Y' in business logic</summary>
    public string ActiveStatus { get; set; } = string.Empty;

    /// <summary>Signed balance; payment zeroes this out. Money — decimal, never double</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Money — decimal, never double</summary>
    public decimal CreditLimit { get; set; }

    /// <summary>Money — decimal, never double</summary>
    public decimal CashCreditLimit { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd</summary>
    public DateOnly OpenDate { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd. Legacy field name has typo; corrected in domain model</summary>
    public DateOnly ExpirationDate { get; set; }

    /// <summary>Proven date field, format yyyy-MM-dd</summary>
    public DateOnly ReissueDate { get; set; }

    /// <summary>Cycle-to-date credit total, updated on each transaction. Money — decimal, never double</summary>
    public decimal CurrentCycleCredit { get; set; }

    /// <summary>Cycle-to-date debit total, updated on each transaction. Money — decimal, never double</summary>
    public decimal CurrentCycleDebit { get; set; }

    /// <summary>Postal/ZIP code stored as text to preserve leading zeros</summary>
    public string AddressZip { get; set; } = string.Empty;

    /// <summary>Account grouping identifier</summary>
    public string GroupId { get; set; } = string.Empty;
}