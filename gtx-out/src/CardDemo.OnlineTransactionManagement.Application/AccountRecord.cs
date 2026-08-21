namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Represents an account record in the system.</summary>
public sealed class AccountRecord
{
    /// <summary>Account identifier.</summary>
    public required long Id { get; set; }

    /// <summary>Account active status. 'Y' means active.</summary>
    public required char ActiveStatus { get; set; }

    /// <summary>Current account balance.</summary>
    public required decimal Balance { get; set; }

    /// <summary>Current cycle credit total.</summary>
    public required decimal CurrentCycleCredit { get; set; }

    /// <summary>Current cycle debit total.</summary>
    public required decimal CurrentCycleDebit { get; set; }
}