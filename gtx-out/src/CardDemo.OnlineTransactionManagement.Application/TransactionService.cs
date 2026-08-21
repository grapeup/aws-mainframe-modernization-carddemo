using System.Globalization;
using System.Text.RegularExpressions;

namespace CardDemo.OnlineTransactionManagement.Application;

/// <summary>
/// Implements online transaction management: adding new transactions and making bill payments.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly IAccountRepository _accounts;
    private readonly ICardRepository _cards;
    private readonly ITransactionRepository _transactions;
    private readonly IUnitOfWork _unitOfWork;

    // Matches an optional sign, one or more digits, and an optional decimal part with exactly 1 or 2 digits.
    private static readonly Regex AmountPattern = new(
        @"^-?\d+(\.\d{1,2})?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public TransactionService(
        IAccountRepository accounts,
        ICardRepository cards,
        ITransactionRepository transactions,
        IUnitOfWork unitOfWork)
    {
        _accounts = accounts;
        _cards = cards;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<AddTransactionResult> AddTransactionAsync(AddTransactionInput input, CancellationToken ct)
    {
        // --- Resolve account and card from whichever identifier was provided ---
        AccountRecord? account;
        CardRecord? card;

        if (!string.IsNullOrWhiteSpace(input.CardNumber))
        {
            card = await _cards.GetByCardNumberAsync(input.CardNumber, ct);
            if (card is null)
                return FailAdd(TransactionError.CardNotFound);

            account = await _accounts.GetByIdAsync(card.AccountId, ct);
            if (account is null)
                return FailAdd(TransactionError.AccountNotFound);
        }
        else if (input.AccountId.HasValue)
        {
            account = await _accounts.GetByIdAsync(input.AccountId.Value, ct);
            if (account is null)
                return FailAdd(TransactionError.AccountNotFound);

            card = await _cards.GetByAccountIdAsync(account.Id, ct);
            if (card is null)
                return FailAdd(TransactionError.CardNotFound);
        }
        else
        {
            return FailAdd(TransactionError.AccountIdMissing);
        }

        // --- Validate account is active ---
        if (account.ActiveStatus != 'Y')
            return FailAdd(TransactionError.AccountNotActive);

        // --- Validate amount format ---
        if (string.IsNullOrWhiteSpace(input.Amount) || !AmountPattern.IsMatch(input.Amount))
            return FailAdd(TransactionError.InvalidAmountFormat);

        if (!decimal.TryParse(input.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
            return FailAdd(TransactionError.InvalidAmountFormat);

        // Round per COBOL ROUNDED semantics
        amount = Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        // --- Begin unit of work so the id read is serialised with the insert ---
        await _unitOfWork.BeginAsync(ct);

        try
        {
            var maxId = await _transactions.GetMaxTransactionIdAsync(ct);
            var newId = maxId + 1;

            // --- Build the transaction record with all FK fields populated ---
            var record = new TransactionRecord
            {
                Id = newId,
                AccountId = account.Id,
                CardNumber = card.CardNumber,
                TypeCode = input.TypeCode,
                CategoryCode = input.CategoryCode,
                Source = input.Source,
                Description = input.Description,
                Amount = amount,
                OriginDate = input.OriginDate,
                ProcessDate = input.ProcessDate,
                MerchantId = input.MerchantId,
                MerchantName = input.MerchantName,
                MerchantCity = input.MerchantCity,
                MerchantZip = input.MerchantZip
            };

            _transactions.StageAdd(record);

            // --- Adjust balance and cycle totals based on transaction type ---
            if (TransactionType.IsCredit(input.TypeCode))
            {
                account.Balance -= amount;
                account.CurrentCycleCredit += amount;
            }
            else
            {
                account.Balance += amount;
                account.CurrentCycleDebit += amount;
            }

            _accounts.StageUpdate(account);

            await _unitOfWork.CommitAsync(ct);

            return new AddTransactionResult
            {
                Success = true,
                Error = TransactionError.None,
                TransactionId = newId,
                UpdatedBalance = account.Balance
            };
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<MakePaymentResult> MakePaymentAsync(MakePaymentInput input, CancellationToken ct)
    {
        // --- Validate account ID is present ---
        if (string.IsNullOrWhiteSpace(input.AccountId))
            return FailPayment(TransactionError.AccountIdMissing);

        // --- Parse account ID ---
        if (!long.TryParse(input.AccountId.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var accountId))
            return FailPayment(TransactionError.AccountNotFound);

        // --- Look up account ---
        var account = await _accounts.GetByIdAsync(accountId, ct);
        if (account is null)
            return FailPayment(TransactionError.AccountNotFound);

        // --- Check balance is positive ---
        if (account.Balance <= 0m)
            return FailPayment(TransactionError.NothingToPay);

        // --- Check confirmation ---
        if (input.Confirmation is not ("Y" or "y"))
            return FailPayment(TransactionError.PaymentNotConfirmed);

        // --- Look up card (mandatory FK) ---
        var card = await _cards.GetByAccountIdAsync(account.Id, ct);
        if (card is null)
            return FailPayment(TransactionError.CardNotFound);

        var paymentAmount = account.Balance;

        // --- Begin unit of work so the id read is serialised with the insert ---
        await _unitOfWork.BeginAsync(ct);

        try
        {
            var maxId = await _transactions.GetMaxTransactionIdAsync(ct);
            var newId = maxId + 1;

            var record = new TransactionRecord
            {
                Id = newId,
                AccountId = account.Id,
                CardNumber = card.CardNumber,
                TypeCode = TransactionType.Payment,
                CategoryCode = 2,
                Source = "POS TERM",
                Description = "BILL PAYMENT - ONLINE",
                Amount = paymentAmount,
                OriginDate = DateTime.Today,
                ProcessDate = DateTime.Today
            };

            _transactions.StageAdd(record);

            // Payment is a credit: reduces balance to zero, adds to cycle credit total
            account.Balance = 0m;
            account.CurrentCycleCredit += paymentAmount;

            _accounts.StageUpdate(account);

            await _unitOfWork.CommitAsync(ct);

            return new MakePaymentResult
            {
                Success = true,
                Error = TransactionError.None,
                TransactionId = newId,
                PaymentAmount = paymentAmount
            };
        }
        catch
        {
            await _unitOfWork.RollbackAsync(ct);
            throw;
        }
    }

    private static AddTransactionResult FailAdd(TransactionError error) => new()
    {
        Success = false,
        Error = error
    };

    private static MakePaymentResult FailPayment(TransactionError error) => new()
    {
        Success = false,
        Error = error
    };
}