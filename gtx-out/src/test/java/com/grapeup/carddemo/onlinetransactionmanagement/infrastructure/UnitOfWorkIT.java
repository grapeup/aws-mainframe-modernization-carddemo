package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import jakarta.persistence.EntityManager;
import jakarta.persistence.EntityManagerFactory;
import jakarta.persistence.PersistenceUnit;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;

import java.math.BigDecimal;
import java.time.LocalDate;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
class UnitOfWorkIT {

    @DynamicPropertySource
    static void datasource(DynamicPropertyRegistry registry) {
        String url = System.getenv("CARDDEMO_TEST_DB");
        if (url == null || url.isBlank()) {
            throw new IllegalStateException(
                    "CARDDEMO_TEST_DB is not set; the integration suite needs a real database");
        }
        registry.add("spring.datasource.url", () -> url);
        registry.add("spring.jpa.hibernate.ddl-auto", () -> "validate");
        registry.add("spring.flyway.enabled", () -> "false");
    }

    @PersistenceUnit
    private EntityManagerFactory emf;

    private Account buildAccount(long id) {
        Account a = new Account();
        a.setId(id);
        a.setActiveStatus("Y");
        a.setCurrentBalance(new BigDecimal("500.00"));
        a.setCreditLimit(new BigDecimal("5000.00"));
        a.setCashCreditLimit(new BigDecimal("1500.00"));
        a.setOpenDate(LocalDate.of(2020, 1, 1));
        a.setExpirationDate(LocalDate.of(2025, 12, 31));
        a.setCurrentCycleCredit(BigDecimal.ZERO);
        a.setCurrentCycleDebit(BigDecimal.ZERO);
        a.setAddressZip("10001");
        a.setGroupId("GRP001");
        return a;
    }

    @Test
    void commit_makesBothWritesDurable() {
        long acctId = 60000001L;
        String cardNum = "6000000000000001";
        String txnId = "0000000000100001";

        // Setup: insert parent chain
        EntityManager setupEm = emf.createEntityManager();
        setupEm.getTransaction().begin();
        setupEm.persist(buildAccount(acctId));
        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber(cardNum);
        ref.setCustomerId(6001);
        ref.setAccountId(acctId);
        setupEm.persist(ref);
        setupEm.getTransaction().commit();
        setupEm.close();

        try {
            // Use UnitOfWork to commit a transaction + account update atomically
            EntityManager workEm = emf.createEntityManager();
            JpaUnitOfWork uow = new JpaUnitOfWork(workEm);
            JpaTransactionRepository txnRepo = new JpaTransactionRepository(workEm);
            JpaAccountRepository acctRepo = new JpaAccountRepository(workEm);

            uow.begin();

            Transaction txn = new Transaction();
            txn.setId(txnId);
            txn.setTypeCode("01");
            txn.setCategoryCode((short) 1);
            txn.setSource("ONLINE");
            txn.setDescription("ATOMICITY TEST");
            txn.setAmount(new BigDecimal("50.00"));
            txn.setCardNumber(cardNum);
            txnRepo.save(txn);

            Account acct = acctRepo.findById(acctId).orElseThrow();
            acct.setCurrentBalance(new BigDecimal("450.00"));
            acctRepo.save(acct);

            uow.commit();
            workEm.close();

            // Verify both writes are durable
            EntityManager verifyEm = emf.createEntityManager();
            Transaction loadedTxn = verifyEm.find(Transaction.class, txnId);
            Account loadedAcct = verifyEm.find(Account.class, acctId);
            assertThat(loadedTxn).isNotNull();
            assertThat(loadedTxn.getDescription()).isEqualTo("ATOMICITY TEST");
            assertThat(loadedAcct.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("450.00"));
            verifyEm.close();
        } finally {
            // Cleanup: children before parents
            EntityManager cleanEm = emf.createEntityManager();
            cleanEm.getTransaction().begin();
            Transaction t = cleanEm.find(Transaction.class, txnId);
            if (t != null) cleanEm.remove(t);
            CardCrossReference c = cleanEm.find(CardCrossReference.class, cardNum);
            if (c != null) cleanEm.remove(c);
            Account a = cleanEm.find(Account.class, acctId);
            if (a != null) cleanEm.remove(a);
            cleanEm.getTransaction().commit();
            cleanEm.close();
        }
    }

    @Test
    void rollback_discardsAllStagedWrites() {
        long acctId = 60000002L;
        String cardNum = "6000000000000002";
        String txnId = "0000000000100002";

        // Setup: insert parent chain
        EntityManager setupEm = emf.createEntityManager();
        setupEm.getTransaction().begin();
        setupEm.persist(buildAccount(acctId));
        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber(cardNum);
        ref.setCustomerId(6002);
        ref.setAccountId(acctId);
        setupEm.persist(ref);
        setupEm.getTransaction().commit();
        setupEm.close();

        try {
            // Use UnitOfWork, stage writes, then rollback
            EntityManager workEm = emf.createEntityManager();
            JpaUnitOfWork uow = new JpaUnitOfWork(workEm);
            JpaTransactionRepository txnRepo = new JpaTransactionRepository(workEm);
            JpaAccountRepository acctRepo = new JpaAccountRepository(workEm);

            uow.begin();

            Transaction txn = new Transaction();
            txn.setId(txnId);
            txn.setTypeCode("01");
            txn.setCategoryCode((short) 1);
            txn.setSource("ONLINE");
            txn.setDescription("ROLLBACK TEST");
            txn.setAmount(new BigDecimal("75.00"));
            txn.setCardNumber(cardNum);
            txnRepo.save(txn);

            Account acct = acctRepo.findById(acctId).orElseThrow();
            acct.setCurrentBalance(new BigDecimal("0.00"));
            acctRepo.save(acct);

            uow.rollback();
            workEm.close();

            // Verify nothing was persisted
            EntityManager verifyEm = emf.createEntityManager();
            Transaction loadedTxn = verifyEm.find(Transaction.class, txnId);
            Account loadedAcct = verifyEm.find(Account.class, acctId);
            assertThat(loadedTxn).isNull();
            // Account balance should be unchanged (original 500.00)
            assertThat(loadedAcct.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("500.00"));
            verifyEm.close();
        } finally {
            // Cleanup
            EntityManager cleanEm = emf.createEntityManager();
            cleanEm.getTransaction().begin();
            CardCrossReference c = cleanEm.find(CardCrossReference.class, cardNum);
            if (c != null) cleanEm.remove(c);
            Account a = cleanEm.find(Account.class, acctId);
            if (a != null) cleanEm.remove(a);
            cleanEm.getTransaction().commit();
            cleanEm.close();
        }
    }
}
