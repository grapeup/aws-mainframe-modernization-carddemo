using CardDemo.OnlineTransactionManagement.Application;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class MakePaymentTests
{
    private InMemoryAccountRepository _accounts = null!;
    private InMemoryCardRepository _cards = null!;
    private InMemoryTransactionRepository _transactions = null!;
    private InMemoryUnitOfWork _unitOfWork = null!;

    private ITransactionService CreateService()
    {
        _accounts = new InMemoryAccountRepository();
        _cards = new InMemoryCardRepository();
        _transactions = new InMemoryTransactionRepository();
        _unitOfWork = new InMemoryUnitOfWork(_accounts, _transactions);
        return new TransactionService(_accounts, _cards, _transactions, _unitOfWork);
    }

    private void SeedAccountWithBalance(long accountId, decimal balance)
    {
        _accounts.Seed(new AccountRecord
        {
            Id = accountId,
            ActiveStatus = 'Y',
            Balance = balance,
            CurrentCycleCredit = 0m,
            CurrentCycleDebit = 0m
        });
        _cards.Seed(new CardRecord
        {
            CardNumber = "4000000000000001",
            AccountId = accountId,
            ActiveStatus = 'Y'
        });
    }

    /// <summary>
    /// Criterion 1: Authenticated user with positive balance makes a payment.
    /// System creates a payment transaction, reduces balance to zero, and returns success with transaction ID.
    /// </summary>
    [Fact]
    public async Task MakePayment_ValidAccountWithBalance_PaysOffAndReturnsSuccess()
    {
        var svc = CreateService();
        SeedAccountWithBalance(10000000001, 350.75m);
        _transactions.Seed(new TransactionRecord
        {
            Id = 200,
            AccountId = 10000000001,
            CardNumber = "4000000000000001",
            TypeCode = "03",
            CategoryCode = 1,
            Source = "ONLINE",
            Description = "SEED",
            Amount = 10m,
            OriginDate = DateTime.Today,
            ProcessDate = DateTime.Today
        });

        var input = new MakePaymentInput { AccountId = "10000000001", Confirmation = "Y" };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(TransactionError.None, result.Error);
        Assert.Equal(201L, result.TransactionId);
        Assert.Equal(350.75m, result.PaymentAmount);

        // Balance should be zero
        var acct = _accounts.GetCommitted(10000000001);
        Assert.NotNull(acct);
        Assert.Equal(0m, acct.Balance);

        // Verify the payment transaction record
        var txn = _transactions.AllCommitted.First(t => t.Id == 201);
        Assert.Equal("02", txn.TypeCode);
        Assert.Equal(2, txn.CategoryCode);
        Assert.Equal("POS TERM", txn.Source);
        Assert.Equal("BILL PAYMENT - ONLINE", txn.Description);
        Assert.Equal(350.75m, txn.Amount);
        Assert.Equal("4000000000000001", txn.CardNumber);
        Assert.Equal(10000000001, txn.AccountId);

        Assert.True(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 2: When no account ID is entered, the system rejects with AccountIdMissing.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task MakePayment_EmptyAccountId_ReturnsAccountIdMissing(string? accountId)
    {
        var svc = CreateService();

        var input = new MakePaymentInput { AccountId = accountId, Confirmation = "Y" };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.AccountIdMissing, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 3: When the account ID does not exist, the system rejects with AccountNotFound.
    /// </summary>
    [Fact]
    public async Task MakePayment_NonExistentAccount_ReturnsAccountNotFound()
    {
        var svc = CreateService();

        var input = new MakePaymentInput { AccountId = "99999999999", Confirmation = "Y" };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.AccountNotFound, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 4: When the account balance is zero or negative, the system rejects with NothingToPay.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-50.00)]
    public async Task MakePayment_ZeroOrNegativeBalance_ReturnsNothingToPay(decimal balance)
    {
        var svc = CreateService();
        SeedAccountWithBalance(10000000001, balance);

        var input = new MakePaymentInput { AccountId = "10000000001", Confirmation = "Y" };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.NothingToPay, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 5: When the user has not confirmed with 'Y' or 'y', the system rejects with PaymentNotConfirmed.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("N")]
    [InlineData("yes")]
    public async Task MakePayment_NotConfirmed_ReturnsPaymentNotConfirmed(string? confirmation)
    {
        var svc = CreateService();
        SeedAccountWithBalance(10000000001, 100.00m);

        var input = new MakePaymentInput { AccountId = "10000000001", Confirmation = confirmation };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.PaymentNotConfirmed, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 6: When payment is confirmed and validated, the system generates a new transaction ID
    /// by incrementing the last transaction ID, creates a payment record with type '02', category 2,
    /// source 'POS TERM', description 'BILL PAYMENT - ONLINE', and amount equal to the current balance.
    /// </summary>
    [Fact]
    public async Task MakePayment_Confirmed_CreatesCorrectPaymentRecord()
    {
        var svc = CreateService();
        SeedAccountWithBalance(10000000001, 999.99m);
        _transactions.Seed(new TransactionRecord
        {
            Id = 500,
            AccountId = 10000000001,
            CardNumber = "4000000000000001",
            TypeCode = "03",
            CategoryCode = 1,
            Source = "ONLINE",
            Description = "SEED",
            Amount = 10m,
            OriginDate = DateTime.Today,
            ProcessDate = DateTime.Today
        });

        var input = new MakePaymentInput { AccountId = "10000000001", Confirmation = "y" };

        var result = await svc.MakePaymentAsync(input, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(501L, result.TransactionId);
        Assert.Equal(999.99m, result.PaymentAmount);

        var txn = _transactions.AllCommitted.First(t => t.Id == 501);
        Assert.Equal("02", txn.TypeCode);
        Assert.Equal(2, txn.CategoryCode);
        Assert.Equal("POS TERM", txn.Source);
        Assert.Equal("BILL PAYMENT - ONLINE", txn.Description);
        Assert.Equal(999.99m, txn.Amount);
        Assert.Equal(10000000001, txn.AccountId);
        Assert.Equal("4000000000000001", txn.CardNumber);

        // Balance should be zero
        var acct = _accounts.GetCommitted(10000000001);
        Assert.NotNull(acct);
        Assert.Equal(0m, acct.Balance);
        Assert.Equal(999.99m, acct.CurrentCycleCredit);

        Assert.True(_unitOfWork.WasBegun);
        Assert.True(_unitOfWork.WasCommitted);
    }
}