namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Error codes for transaction management operations.</summary>
public enum TransactionError
{
    None = 0,
    AccountIdMissing,
    AccountNotFound,
    AccountNotActive,
    CardNotFound,
    InvalidAmountFormat,
    NothingToPay,
    PaymentNotConfirmed
}