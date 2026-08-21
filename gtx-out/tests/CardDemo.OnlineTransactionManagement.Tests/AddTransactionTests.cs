using CardDemo.OnlineTransactionManagement.Application;
using Xunit;

namespace CardDemo.OnlineTransactionManagement.Tests;

public sealed class AddTransactionTests
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

    private static AddTransactionInput ValidInput(long? accountId = null, string? cardNumber = null) => new()
    {
        AccountId = accountId ?? 10000000001,
        CardNumber = cardNumber,
        TypeCode = "03",
        CategoryCode = 5,
        Source = "POS TERM",
        Description = "PURCHASE AT STORE",
        Amount = "125.50",
        OriginDate = new DateTime(2024, 1, 15),
        ProcessDate = new DateTime(2024, 1, 16),
        MerchantId = "M001",
        MerchantName = "Test Merchant",
        MerchantCity = "New York",
        MerchantZip = "10001"
    };

    private void SeedActiveAccountAndCard(long accountId = 10000000001, decimal balance = 500.00m)
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
    /// Criterion 1: Authenticated user with active account and card adds a valid transaction.
    /// System generates a unique transaction ID, creates the record, updates balance and cycle totals,
    /// and returns a success confirmation.
    /// </summary>
    [Fact]
    public async Task AddTransaction_ValidInput_CreatesRecordAndUpdatesBalance()
    {
        var svc = CreateService();
        SeedActiveAccountAndCard();
        _transactions.Seed(new TransactionRecord
        {
            Id = 100,
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

        var result = await svc.AddTransactionAsync(ValidInput(), CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(TransactionError.None, result.Error);
        Assert.Equal(101L, result.TransactionId);

        // Balance should increase for a debit/purchase (type 03)
        // 500 + 125.50 = 625.50
        Assert.Equal(625.50m, result.UpdatedBalance);

        Assert.True(_unitOfWork.WasCommitted);

        // Verify the transaction record was staged and committed
        var committed = _transactions.AllCommitted;
        var newTxn = committed.FirstOrDefault(t => t.Id == 101);
        Assert.NotNull(newTxn);
        Assert.Equal(10000000001, newTxn.AccountId);
        Assert.Equal("4000000000000001", newTxn.CardNumber);
        Assert.Equal(125.50m, newTxn.Amount);

        // Verify account was updated
        var acct = _accounts.GetCommitted(10000000001);
        Assert.NotNull(acct);
        Assert.Equal(625.50m, acct.Balance);
        Assert.Equal(125.50m, acct.CurrentCycleDebit);
    }

    /// <summary>
    /// Criterion 2: User can enter either an 11-character account number or a 16-character card number
    /// and the system validates and retrieves the associated account and card information.
    /// </summary>
    [Fact]
    public async Task AddTransaction_ByCardNumber_ResolvesAccountAndSucceeds()
    {
        var svc = CreateService();
        SeedActiveAccountAndCard();

        var input = ValidInput(accountId: null, cardNumber: "4000000000000001");

        var result = await svc.AddTransactionAsync(input, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.TransactionId > 0);
        Assert.True(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 3: When the account is not active, the system rejects the transaction
    /// with an AccountNotActive error.
    /// </summary>
    [Fact]
    public async Task AddTransaction_InactiveAccount_ReturnsAccountNotActive()
    {
        var svc = CreateService();
        _accounts.Seed(new AccountRecord
        {
            Id = 10000000001,
            ActiveStatus = 'N',
            Balance = 500.00m,
            CurrentCycleCredit = 0m,
            CurrentCycleDebit = 0m
        });
        _cards.Seed(new CardRecord
        {
            CardNumber = "4000000000000001",
            AccountId = 10000000001,
            ActiveStatus = 'Y'
        });

        var result = await svc.AddTransactionAsync(ValidInput(), CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.AccountNotActive, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 4: The system accepts all required transaction detail fields:
    /// type code, category code, source, description, amount, origin date, process date,
    /// merchant ID, merchant name, merchant city, and merchant zip.
    /// </summary>
    [Fact]
    public async Task AddTransaction_AllDetailFields_ArePersistedCorrectly()
    {
        var svc = CreateService();
        SeedActiveAccountAndCard();

        var input = new AddTransactionInput
        {
            AccountId = 10000000001,
            TypeCode = "01",
            CategoryCode = 3,
            Source = "ONLINE",
            Description = "REFUND FOR ORDER",
            Amount = "75.25",
            OriginDate = new DateTime(2024, 3, 10),
            ProcessDate = new DateTime(2024, 3, 11),
            MerchantId = "MERCH99",
            MerchantName = "Acme Corp",
            MerchantCity = "Chicago",
            MerchantZip = "60601"
        };

        var result = await svc.AddTransactionAsync(input, CancellationToken.None);

        Assert.True(result.Success);

        var txn = _transactions.AllCommitted.First(t => t.Id == result.TransactionId);
        Assert.Equal("01", txn.TypeCode);
        Assert.Equal(3, txn.CategoryCode);
        Assert.Equal("ONLINE", txn.Source);
        Assert.Equal("REFUND FOR ORDER", txn.Description);
        Assert.Equal(75.25m, txn.Amount);
        Assert.Equal(new DateTime(2024, 3, 10), txn.OriginDate);
        Assert.Equal(new DateTime(2024, 3, 11), txn.ProcessDate);
        Assert.Equal("MERCH99", txn.MerchantId);
        Assert.Equal("Acme Corp", txn.MerchantName);
        Assert.Equal("Chicago", txn.MerchantCity);
        Assert.Equal("60601", txn.MerchantZip);
    }

    /// <summary>
    /// Criterion 5: When the amount is not a valid numeric value with up to 2 decimal places,
    /// the system rejects the transaction with an InvalidAmountFormat error.
    /// </summary>
    [Theory]
    [InlineData("abc")]
    [InlineData("12.345")]
    [InlineData("")]
    [InlineData("12.3.4")]
    public async Task AddTransaction_InvalidAmount_ReturnsInvalidAmountFormat(string badAmount)
    {
        var svc = CreateService();
        SeedActiveAccountAndCard();

        var input = ValidInput() with { Amount = badAmount };

        var result = await svc.AddTransactionAsync(input, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal(TransactionError.InvalidAmountFormat, result.Error);
        Assert.False(_unitOfWork.WasCommitted);
    }

    /// <summary>
    /// Criterion 6: On successful creation, the system adjusts the account balance based on
    /// the transaction type and updates current cycle credit or debit totals accordingly.
    /// A credit transaction (type 01) reduces the balance and increases cycle credit total.
    /// </summary>
    [Fact]
    public async Task AddTransaction_CreditType_ReducesBalanceAndUpdatesCycleCredit()
    {
        var svc = CreateService();
        SeedActiveAccountAndCard(balance: 1000.00m);

        var input = ValidInput() with { TypeCode = "01", Amount = "200.00" };

        var result = await svc.AddTransactionAsync(input, CancellationToken.None);

        Assert.True(result.Success);
        // Credit reduces balance: 1000 - 200 = 800
        Assert.Equal(800.00m, result.UpdatedBalance);

        var acct = _accounts.GetCommitted(10000000001);
        Assert.NotNull(acct);
        Assert.Equal(800.00m, acct.Balance);
        Assert.Equal(200.00m, acct.CurrentCycleCredit);
        Assert.Equal(0m, acct.CurrentCycleDebit);
    }
}