namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Indicates whether a transaction is a credit or debit for balance adjustment purposes.</summary>
public static class TransactionType
{
    /// <summary>Credit transaction type code.</summary>
    public const string Credit = "01";

    /// <summary>Payment / credit transaction type code.</summary>
    public const string Payment = "02";

    /// <summary>Debit / purchase transaction type code.</summary>
    public const string Purchase = "03";

    /// <summary>Returns true when the type code represents a credit (reduces balance).</summary>
    public static bool IsCredit(string typeCode) => typeCode == Credit || typeCode == Payment;
}