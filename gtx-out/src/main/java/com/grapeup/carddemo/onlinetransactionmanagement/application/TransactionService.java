package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Merchant;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.OffsetDateTime;
import java.time.ZoneOffset;
import java.util.Optional;
import java.util.regex.Pattern;

/**
 * Implements the online transaction management use cases:
 * adding a new transaction and making an online bill payment.
 */
public final class TransactionService implements TransactionUseCase {

    private static final String CREDIT_TYPE_CODE = "02";
    private static final int TRANSACTION_ID_LENGTH = 16;
    private static final Pattern VALID_AMOUNT = Pattern.compile("^\\d+(\\.\\d{1,2})?$");
    private static final int ACCOUNT_ID_LENGTH = 11;
    private static final int CARD_NUMBER_LENGTH = 16;

    private final AccountRepository accounts;
    private final CardCrossReferenceRepository cards;
    private final TransactionRepository transactions;
    private final UnitOfWork unitOfWork;

    public TransactionService(AccountRepository accounts,
                              CardCrossReferenceRepository cards,
                              TransactionRepository transactions,
                              UnitOfWork unitOfWork) {
        this.accounts = accounts;
        this.cards = cards;
        this.transactions = transactions;
        this.unitOfWork = unitOfWork;
    }

    @Override
    public AddTransactionResult addTransaction(AddTransactionInput input) {
        if (!VALID_AMOUNT.matcher(input.amount()).matches()) {
            return AddTransactionResult.fail(TransactionError.INVALID_AMOUNT_FORMAT);
        }

        BigDecimal amount = new BigDecimal(input.amount());

        String lookup = input.accountIdOrCardNumber();
        Long accountId;
        String cardNumber;

        if (lookup.length() == CARD_NUMBER_LENGTH) {
            Optional<CardCrossReference> ref = cards.findByCardNumber(lookup);
            if (ref.isEmpty()) {
                return AddTransactionResult.fail(TransactionError.CARD_NOT_FOUND);
            }
            accountId = ref.get().getAccountId();
            cardNumber = ref.get().getCardNumber();
        } else {
            accountId = Long.parseLong(lookup);
            cardNumber = input.cardNumber();
        }

        Optional<Account> maybeAccount = accounts.findById(accountId);
        if (maybeAccount.isEmpty()) {
            return AddTransactionResult.fail(TransactionError.ACCOUNT_NOT_FOUND);
        }

        Account account = maybeAccount.get();

        if (!"Y".equals(account.getActiveStatus())) {
            return AddTransactionResult.fail(TransactionError.ACCOUNT_NOT_ACTIVE);
        }

        if (cardNumber == null || cardNumber.isBlank()) {
            Optional<CardCrossReference> ref = cards.findByAccountId(accountId);
            if (ref.isEmpty()) {
                return AddTransactionResult.fail(TransactionError.CARD_NOT_FOUND);
            }
            cardNumber = ref.get().getCardNumber();
        }

        unitOfWork.begin();
        try {
            String newId = nextTransactionId();

            Transaction txn = new Transaction();
            txn.setId(newId);
            txn.setTypeCode(input.typeCode());
            txn.setCategoryCode(input.categoryCode());
            txn.setSource(input.source());
            txn.setDescription(input.description());
            txn.setAmount(amount);
            txn.setCardNumber(cardNumber);

            if (input.originDate() != null) {
                LocalDate originDate = LocalDate.parse(input.originDate());
                txn.setOriginatedAt(originDate.atStartOfDay().atOffset(ZoneOffset.UTC));
            }
            if (input.processDate() != null) {
                LocalDate processDate = LocalDate.parse(input.processDate());
                txn.setProcessedAt(processDate.atStartOfDay().atOffset(ZoneOffset.UTC));
            }

            if (input.merchantId() != null || input.merchantName() != null
                    || input.merchantCity() != null || input.merchantZip() != null) {
                Merchant merchant = new Merchant();
                merchant.setId(input.merchantId());
                merchant.setName(input.merchantName());
                merchant.setCity(input.merchantCity());
                merchant.setZip(input.merchantZip());
                txn.setMerchant(merchant);
            }

            adjustBalance(account, amount, input.typeCode());

            transactions.save(txn);
            accounts.save(account);
            unitOfWork.commit();

            return AddTransactionResult.ok(newId, account.getCurrentBalance());
        } catch (Exception e) {
            unitOfWork.rollback();
            throw e;
        }
    }

    @Override
    public PaymentResult makePayment(PaymentInput input) {
        if (input.accountId() == null || input.accountId().isBlank()) {
            return PaymentResult.fail(TransactionError.ACCOUNT_ID_MISSING);
        }

        Long accountId = Long.parseLong(input.accountId());

        Optional<Account> maybeAccount = accounts.findById(accountId);
        if (maybeAccount.isEmpty()) {
            return PaymentResult.fail(TransactionError.ACCOUNT_NOT_FOUND);
        }

        Account account = maybeAccount.get();

        if (account.getCurrentBalance().compareTo(BigDecimal.ZERO) <= 0) {
            return PaymentResult.fail(TransactionError.NOTHING_TO_PAY);
        }

        if (!isConfirmed(input.confirmation())) {
            return PaymentResult.fail(TransactionError.PAYMENT_NOT_CONFIRMED);
        }

        Optional<CardCrossReference> cardRef = cards.findByAccountId(accountId);
        if (cardRef.isEmpty()) {
            return PaymentResult.fail(TransactionError.CARD_NOT_FOUND);
        }
        String cardNumber = cardRef.get().getCardNumber();

        unitOfWork.begin();
        try {
            String newId = nextTransactionId();
            BigDecimal paymentAmount = account.getCurrentBalance();

            Transaction txn = new Transaction();
            txn.setId(newId);
            txn.setTypeCode("02");
            txn.setCategoryCode((short) 2);
            txn.setSource("POS TERM");
            txn.setDescription("BILL PAYMENT - ONLINE");
            txn.setAmount(paymentAmount);
            txn.setCardNumber(cardNumber);

            OffsetDateTime now = OffsetDateTime.now(ZoneOffset.UTC);
            txn.setOriginatedAt(now);
            txn.setProcessedAt(now);

            account.setCurrentBalance(BigDecimal.ZERO);
            BigDecimal cycleCredit = account.getCurrentCycleCredit() != null
                    ? account.getCurrentCycleCredit()
                    : BigDecimal.ZERO;
            account.setCurrentCycleCredit(cycleCredit.add(paymentAmount));

            transactions.save(txn);
            accounts.save(account);
            unitOfWork.commit();

            return PaymentResult.ok(newId, paymentAmount);
        } catch (Exception e) {
            unitOfWork.rollback();
            throw e;
        }
    }

    private void adjustBalance(Account account, BigDecimal amount, String typeCode) {
        BigDecimal currentBalance = account.getCurrentBalance() != null
                ? account.getCurrentBalance()
                : BigDecimal.ZERO;

        if (CREDIT_TYPE_CODE.equals(typeCode)) {
            account.setCurrentBalance(currentBalance.subtract(amount));
            BigDecimal cycleCredit = account.getCurrentCycleCredit() != null
                    ? account.getCurrentCycleCredit()
                    : BigDecimal.ZERO;
            account.setCurrentCycleCredit(cycleCredit.add(amount));
        } else {
            account.setCurrentBalance(currentBalance.add(amount));
            BigDecimal cycleDebit = account.getCurrentCycleDebit() != null
                    ? account.getCurrentCycleDebit()
                    : BigDecimal.ZERO;
            account.setCurrentCycleDebit(cycleDebit.add(amount));
        }
    }

    private String nextTransactionId() {
        Optional<String> maxId = transactions.findMaxTransactionId();
        long next = maxId.map(Long::parseLong).orElse(0L) + 1;
        return String.format("%0" + TRANSACTION_ID_LENGTH + "d", next);
    }

    private boolean isConfirmed(String confirmation) {
        return "Y".equalsIgnoreCase(confirmation);
    }
}
