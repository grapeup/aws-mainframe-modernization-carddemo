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
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
class ConcurrentIdGenerationIT {

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

    @Test
    void concurrentInserts_produceUniqueIds() throws Exception {
        int threadCount = 5;
        int maxRetries = 3;
        long baseAcctId = 50000000L;

        // Setup: create one account and card per thread
        EntityManager setupEm = emf.createEntityManager();
        setupEm.getTransaction().begin();
        for (int i = 0; i < threadCount; i++) {
            Account a = new Account();
            a.setId(baseAcctId + i);
            a.setActiveStatus("Y");
            a.setCurrentBalance(new BigDecimal("1000.00"));
            a.setCreditLimit(new BigDecimal("5000.00"));
            a.setCashCreditLimit(new BigDecimal("1500.00"));
            a.setOpenDate(LocalDate.of(2020, 1, 1));
            a.setExpirationDate(LocalDate.of(2025, 12, 31));
            a.setCurrentCycleCredit(BigDecimal.ZERO);
            a.setCurrentCycleDebit(BigDecimal.ZERO);
            a.setAddressZip("10001");
            a.setGroupId("GRP001");
            setupEm.persist(a);

            CardCrossReference ref = new CardCrossReference();
            ref.setCardNumber(String.format("3%015d", i + 1));
            ref.setCustomerId(5000 + i);
            ref.setAccountId(baseAcctId + i);
            setupEm.persist(ref);
        }
        setupEm.getTransaction().commit();
        setupEm.close();

        Set<String> generatedIds = ConcurrentHashMap.newKeySet();
        CountDownLatch startLatch = new CountDownLatch(1);
        ExecutorService executor = Executors.newFixedThreadPool(threadCount);
        List<Future<String>> futures = new ArrayList<>();

        try {
            for (int i = 0; i < threadCount; i++) {
                final int idx = i;
                futures.add(executor.submit(() -> {
                    startLatch.await();
                    for (int attempt = 0; attempt < maxRetries; attempt++) {
                        EntityManager workEm = emf.createEntityManager();
                        try {
                            JpaUnitOfWork uow = new JpaUnitOfWork(workEm);
                            JpaTransactionRepository txnRepo = new JpaTransactionRepository(workEm);

                            uow.begin();
                            String newId = nextTransactionId(txnRepo);

                            Transaction txn = new Transaction();
                            txn.setId(newId);
                            txn.setTypeCode("01");
                            txn.setCategoryCode((short) 1);
                            txn.setSource("ONLINE");
                            txn.setDescription("CONCURRENT " + idx);
                            txn.setAmount(new BigDecimal("10.00"));
                            txn.setCardNumber(String.format("3%015d", idx + 1));
                            txnRepo.save(txn);

                            uow.commit();
                            workEm.close();
                            generatedIds.add(newId);
                            return newId;
                        } catch (Exception e) {
                            try {
                                if (workEm.getTransaction().isActive()) {
                                    workEm.getTransaction().rollback();
                                }
                            } catch (IllegalStateException ise) {
                                // ignore
                            }
                            workEm.close();
                            if (attempt == maxRetries - 1) {
                                throw e;
                            }
                            // Retry on contention
                        }
                    }
                    throw new RuntimeException("Exhausted retries");
                }));
            }

            startLatch.countDown();

            for (Future<String> f : futures) {
                f.get(30, TimeUnit.SECONDS);
            }

            // All generated IDs must be unique
            assertThat(generatedIds).hasSize(threadCount);
        } finally {
            executor.shutdownNow();

            // Cleanup: delete transactions, then cards, then accounts
            EntityManager cleanEm = emf.createEntityManager();
            cleanEm.getTransaction().begin();
            for (String id : generatedIds) {
                Transaction t = cleanEm.find(Transaction.class, id);
                if (t != null) cleanEm.remove(t);
            }
            for (int i = 0; i < threadCount; i++) {
                String cn = String.format("3%015d", i + 1);
                CardCrossReference c = cleanEm.find(CardCrossReference.class, cn);
                if (c != null) cleanEm.remove(c);
                Account a = cleanEm.find(Account.class, baseAcctId + i);
                if (a != null) cleanEm.remove(a);
            }
            cleanEm.getTransaction().commit();
            cleanEm.close();
        }
    }

    private String nextTransactionId(JpaTransactionRepository repo) {
        java.util.Optional<String> maxId = repo.findMaxTransactionId();
        long next = maxId.map(s -> Long.parseLong(s.trim())).orElse(0L) + 1;
        return String.format("%016d", next);
    }
}
