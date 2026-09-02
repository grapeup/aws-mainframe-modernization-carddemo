package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import java.math.BigDecimal;
import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Acceptance tests for the Make Online Payment story.
 */
class MakePaymentTest {

    private InMemoryAccountRepository accounts;
    private InMemoryCardCrossReferenceRepository cards;
    private InMemoryTransactionRepository transactions;
    private InMemoryUnitOfWork unitOfWork;
    private TransactionUseCase service;

    private static TransactionUseCase createService(
            AccountRepository accounts, CardCrossReferenceRepository cards,
            TransactionRepository transactions, UnitOfWork unitOfWork) {
        return new TransactionService(accounts, cards, transactions, unitOfWork);
    }

    @BeforeEach
    void setUp() {
        accounts = new InMemoryAccountRepository();
        cards = new InMemoryCardCrossReferenceRepository();
        transactions = new InMemoryTransactionRepository();
        unitOfWork = new InMemoryUnitOfWork();
        service = createService(accounts, cards, transactions, unitOfWork);
    }

    private Account activeAccount(Long id, BigDecimal balance) {
        Account a = new Account();
        a.setId(id);
        a.setActiveStatus("Y");
        a.setCurrentBalance(balance);
        a.setCreditLimit(new BigDecimal("5000.00"));
        a.setCashCreditLimit(new BigDecimal("1000.00"));
        a.setOpenDate(LocalDate.of(2020, 1, 1));
        a.setExpirationDate(LocalDate.of(2030, 12, 31));
        a.setCurrentCycleCredit(BigDecimal.ZERO);
        a.setCurrentCycleDebit(BigDecimal.ZERO);
        return a;
    }

    private CardCrossReference cardRef(String cardNumber, Long accountId) {
        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber(cardNumber);
        ref.setAccountId(accountId);
        ref.setCustomerId(1001);
        return ref;
    }

    private void seedExistingTransaction(String id) {
        Transaction t = new Transaction();
        t.setId(id);
        t.setTypeCode("01");
        t.setCategoryCode((short) 1);
        t.setSource("ONLINE");
        t.setDescription("SEED");
        t.setAmount(BigDecimal.ONE);
        t.setCardNumber("4000000000000001");
        transactions.seed(t);
    }

    /**
     * Criterion 1: Successful payment.
     * Given I am authenticated and my account has a positive balance,
     * when I enter my account ID, confirm the payment, and submit,
     * then the system creates a payment transaction, reduces my account balance to zero,
     * and displays a success message with the transaction ID.
     */
    @Test
    void successfulPaymentReducesBalanceToZero() {
        Account acct = activeAccount(12345678901L, new BigDecimal("500.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000100");

        PaymentInput input = new PaymentInput("12345678901", "Y");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isTrue();
        assertThat(result.transactionId()).isNotNull().isNotBlank();
        assertThat(result.amountPaid()).isEqualByComparingTo(new BigDecimal("500.00"));

        // Account balance is now zero
        Account updated = accounts.getStored(12345678901L);
        assertThat(updated.getCurrentBalance()).isEqualByComparingTo(BigDecimal.ZERO);

        // Transaction record was created
        Transaction saved = transactions.getStored(result.transactionId());
        assertThat(saved).isNotNull();
        assertThat(saved.getAmount()).isEqualByComparingTo(new BigDecimal("500.00"));

        // Unit of work was committed
        assertThat(unitOfWork.wasCommitted()).isTrue();
    }

    /**
     * Criterion 2: Empty account ID.
     * Given I do not enter an account ID,
     * when I attempt to proceed,
     * then the system displays an error message 'Acct ID can NOT be empty'.
     */
    @Test
    void emptyAccountIdIsRejected() {
        PaymentInput input = new PaymentInput("", "Y");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.ACCOUNT_ID_MISSING);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 3: Account not found.
     * Given I enter an account ID that does not exist,
     * when I submit the payment request,
     * then the system displays an error message 'Account ID NOT found'.
     */
    @Test
    void nonExistentAccountIsRejected() {
        PaymentInput input = new PaymentInput("99999999999", "Y");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.ACCOUNT_NOT_FOUND);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 4: Zero or negative balance.
     * Given my account balance is zero or negative,
     * when I attempt to make a payment,
     * then the system displays an error message 'You have nothing to pay'.
     */
    @Test
    void zeroBalanceAccountCannotPay() {
        Account acct = activeAccount(12345678901L, BigDecimal.ZERO);
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);

        PaymentInput input = new PaymentInput("12345678901", "Y");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.NOTHING_TO_PAY);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 5: Payment not confirmed.
     * Given I have not entered 'Y' or 'y' in the confirmation field,
     * when I attempt to submit the payment,
     * then the system displays a message 'Confirm to make a bill payment'.
     */
    @Test
    void unconfirmedPaymentIsRejected() {
        Account acct = activeAccount(12345678901L, new BigDecimal("500.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);

        PaymentInput input = new PaymentInput("12345678901", "N");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.PAYMENT_NOT_CONFIRMED);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 6: Payment transaction record details.
     * Given my payment is confirmed and validated,
     * when the system processes the payment,
     * then the system generates a new transaction ID by incrementing the last transaction ID,
     * creates a payment transaction record with type code '02', category code 2,
     * source 'POS TERM', description 'BILL PAYMENT - ONLINE',
     * and amount equal to my current balance.
     */
    @Test
    void paymentTransactionRecordHasCorrectDetails() {
        Account acct = activeAccount(12345678901L, new BigDecimal("750.50"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000099");

        PaymentInput input = new PaymentInput("12345678901", "y");

        PaymentResult result = service.makePayment(input);

        assertThat(result.success()).isTrue();

        // Transaction ID is the previous max + 1
        assertThat(result.transactionId()).isEqualTo("0000000000000100");

        Transaction saved = transactions.getStored(result.transactionId());
        assertThat(saved).isNotNull();
        assertThat(saved.getTypeCode()).isEqualTo("02");
        assertThat(saved.getCategoryCode()).isEqualTo((short) 2);
        assertThat(saved.getSource()).isEqualTo("POS TERM");
        assertThat(saved.getDescription()).isEqualTo("BILL PAYMENT - ONLINE");
        assertThat(saved.getAmount()).isEqualByComparingTo(new BigDecimal("750.50"));
        assertThat(saved.getCardNumber()).isEqualTo("4000000000000001");
    }
}
