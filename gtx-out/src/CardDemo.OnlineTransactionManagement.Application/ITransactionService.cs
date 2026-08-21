namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>Service interface for online transaction management.</summary>
public interface ITransactionService
{
    /// <summary>Adds a new transaction to the system.</summary>
    Task<AddTransactionResult> AddTransactionAsync(AddTransactionInput input, CancellationToken ct);

    /// <summary>Makes an online bill payment that pays off the full account balance.</summary>
    Task<MakePaymentResult> MakePaymentAsync(MakePaymentInput input, CancellationToken ct);
}