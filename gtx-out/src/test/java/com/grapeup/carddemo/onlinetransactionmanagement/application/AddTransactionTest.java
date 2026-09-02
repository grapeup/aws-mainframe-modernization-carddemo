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
 * Acceptance tests for the Add New Transaction story.
 */
class AddTransactionTest {

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
     * Criterion 1: Successful transaction creation.
     * Given I am authenticated and the account and card are active,
     * when I enter valid transaction details, confirm the addition, and submit,
     * then the system generates a unique transaction ID, creates the transaction record,
     * updates the account balance and cycle totals, and displays a success confirmation.
     */
    @Test
    void successfulTransactionCreatesRecordAndUpdatesBalance() {
        Account acct = activeAccount(12345678901L, new BigDecimal("1000.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000100");

        AddTransactionInput input = new AddTransactionInput(
                "12345678901",
                "01",
                (short) 5,
                "ONLINE",
                "PURCHASE AT STORE",
                "150.00",
                "2024-01-15",
                "2024-01-16",
                12345,
                "ACME STORE",
                "NEW YORK",
                "10001",
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isTrue();
        assertThat(result.transactionId()).isNotNull().isNotBlank();
        assertThat(result.error()).isNull();

        // Transaction record was created
        Transaction saved = transactions.getStored(result.transactionId());
        assertThat(saved).isNotNull();
        assertThat(saved.getTypeCode()).isEqualTo("01");
        assertThat(saved.getCategoryCode()).isEqualTo((short) 5);
        assertThat(saved.getSource()).isEqualTo("ONLINE");
        assertThat(saved.getDescription()).isEqualTo("PURCHASE AT STORE");
        assertThat(saved.getAmount()).isEqualByComparingTo(new BigDecimal("150.00"));
        assertThat(saved.getCardNumber()).isEqualTo("4000000000000001");

        // Account balance was updated (debit increases balance)
        Account updated = accounts.getStored(12345678901L);
        assertThat(updated.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("1150.00"));
        assertThat(updated.getCurrentCycleDebit()).isEqualByComparingTo(new BigDecimal("150.00"));

        // Unit of work was committed
        assertThat(unitOfWork.wasCommitted()).isTrue();
    }

    /**
     * Criterion 2: Account or card number lookup.
     * Given I need to add a transaction,
     * when I enter either an 11-character account number or a 16-character card number,
     * then the system validates the input and retrieves the associated account and card information.
     */
    @Test
    void lookupByCardNumberRetrievesAccountAndCard() {
        Account acct = activeAccount(12345678901L, new BigDecimal("500.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000050");

        // Use 16-character card number as the lookup key
        AddTransactionInput input = new AddTransactionInput(
                "4000000000000001",
                "01",
                (short) 3,
                "POS TERM",
                "CARD LOOKUP TEST",
                "25.00",
                "2024-02-01",
                "2024-02-02",
                99999,
                "SHOP",
                "BOSTON",
                "02101",
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isTrue();
        Transaction saved = transactions.getStored(result.transactionId());
        assertThat(saved).isNotNull();
        assertThat(saved.getCardNumber()).isEqualTo("4000000000000001");

        // Account balance was updated, proving the account was found via the card
        Account updated = accounts.getStored(12345678901L);
        assertThat(updated.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("525.00"));
    }

    /**
     * Criterion 3: Inactive account rejection.
     * Given the account is not active,
     * when I attempt to add a transaction,
     * then the system displays an error message 'Account is not active'.
     */
    @Test
    void inactiveAccountIsRejected() {
        Account acct = activeAccount(12345678901L, new BigDecimal("1000.00"));
        acct.setActiveStatus("N");
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);

        AddTransactionInput input = new AddTransactionInput(
                "12345678901",
                "01",
                (short) 1,
                "ONLINE",
                "TEST",
                "50.00",
                "2024-01-01",
                "2024-01-02",
                null,
                null,
                null,
                null,
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.ACCOUNT_NOT_ACTIVE);

        // No transaction was created
        assertThat(transactions.size()).isEqualTo(0);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 4: Transaction detail fields are accepted.
     * Given the account and card are validated,
     * when the system displays the transaction detail fields,
     * then I can enter type code, category code, source, description, amount,
     * origin date, process date, merchant ID, merchant name, merchant city, and merchant zip.
     */
    @Test
    void allTransactionDetailFieldsArePersistedCorrectly() {
        Account acct = activeAccount(12345678901L, new BigDecimal("2000.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000001");

        AddTransactionInput input = new AddTransactionInput(
                "12345678901",
                "03",
                (short) 7,
                "WEB",
                "ONLINE PURCHASE",
                "299.99",
                "2024-03-10",
                "2024-03-11",
                54321,
                "MEGA MART",
                "CHICAGO",
                "60601",
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isTrue();
        Transaction saved = transactions.getStored(result.transactionId());
        assertThat(saved).isNotNull();
        assertThat(saved.getTypeCode()).isEqualTo("03");
        assertThat(saved.getCategoryCode()).isEqualTo((short) 7);
        assertThat(saved.getSource()).isEqualTo("WEB");
        assertThat(saved.getDescription()).isEqualTo("ONLINE PURCHASE");
        assertThat(saved.getAmount()).isEqualByComparingTo(new BigDecimal("299.99"));
        assertThat(saved.getCardNumber()).isEqualTo("4000000000000001");
        assertThat(saved.getMerchant()).isNotNull();
        assertThat(saved.getMerchant().getId()).isEqualTo(54321);
        assertThat(saved.getMerchant().getName()).isEqualTo("MEGA MART");
        assertThat(saved.getMerchant().getCity()).isEqualTo("CHICAGO");
        assertThat(saved.getMerchant().getZip()).isEqualTo("60601");
    }

    /**
     * Criterion 5: Invalid amount format.
     * Given I enter an amount that is not a valid numeric value with up to 2 decimal places,
     * when I submit the transaction,
     * then the system displays an error message 'Invalid amount format'.
     */
    @Test
    void invalidAmountFormatIsRejected() {
        Account acct = activeAccount(12345678901L, new BigDecimal("1000.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);

        AddTransactionInput input = new AddTransactionInput(
                "12345678901",
                "01",
                (short) 1,
                "ONLINE",
                "TEST",
                "abc.xyz",
                "2024-01-01",
                "2024-01-02",
                null,
                null,
                null,
                null,
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isFalse();
        assertThat(result.error()).isEqualTo(TransactionError.INVALID_AMOUNT_FORMAT);
        assertThat(unitOfWork.wasCommitted()).isFalse();
    }

    /**
     * Criterion 6: Balance and cycle totals updated based on transaction type.
     * Given the transaction is created successfully,
     * when the system updates the account,
     * then the system adjusts the account balance based on the transaction type
     * and updates current cycle credit or debit totals accordingly.
     * <p>
     * Type code '02' (payment/credit) reduces the balance and increases cycle credit.
     * Other type codes (e.g. '01' purchase/debit) increase the balance and increase cycle debit.
     * </p>
     */
    @Test
    void creditTransactionReducesBalanceAndUpdatesCycleCredit() {
        Account acct = activeAccount(12345678901L, new BigDecimal("1000.00"));
        accounts.seed(acct);
        CardCrossReference ref = cardRef("4000000000000001", 12345678901L);
        cards.seed(ref);
        seedExistingTransaction("0000000000000010");

        // Type '02' is a credit/payment type
        AddTransactionInput input = new AddTransactionInput(
                "12345678901",
                "02",
                (short) 2,
                "POS TERM",
                "PAYMENT RECEIVED",
                "200.00",
                "2024-04-01",
                "2024-04-02",
                null,
                null,
                null,
                null,
                "4000000000000001"
        );

        AddTransactionResult result = service.addTransaction(input);

        assertThat(result.success()).isTrue();

        Account updated = accounts.getStored(12345678901L);
        // Credit reduces balance
        assertThat(updated.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("800.00"));
        // Cycle credit updated
        assertThat(updated.getCurrentCycleCredit()).isEqualByComparingTo(new BigDecimal("200.00"));
        // Cycle debit unchanged
        assertThat(updated.getCurrentCycleDebit()).isEqualByComparingTo(BigDecimal.ZERO);
    }
}
