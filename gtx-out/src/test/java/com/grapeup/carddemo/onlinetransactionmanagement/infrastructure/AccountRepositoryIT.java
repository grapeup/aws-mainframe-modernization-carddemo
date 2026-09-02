package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@Transactional
class AccountRepositoryIT {

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

    @PersistenceContext
    private EntityManager em;

    private Account buildAccount(long id) {
        Account a = new Account();
        a.setId(id);
        a.setActiveStatus("Y");
        a.setCurrentBalance(new BigDecimal("1234.56"));
        a.setCreditLimit(new BigDecimal("5000.00"));
        a.setCashCreditLimit(new BigDecimal("1500.00"));
        a.setOpenDate(LocalDate.of(2020, 1, 15));
        a.setExpirationDate(LocalDate.of(2025, 12, 31));
        a.setCurrentCycleCredit(new BigDecimal("100.00"));
        a.setCurrentCycleDebit(new BigDecimal("200.00"));
        a.setAddressZip("07001");
        a.setGroupId("GRP001");
        return a;
    }

    @Test
    void findById_returnsPersistedAccount() {
        Account a = buildAccount(90000001L);
        em.persist(a);
        em.flush();
        em.clear();

        JpaAccountRepository repo = new JpaAccountRepository(em);
        Optional<Account> found = repo.findById(90000001L);

        assertThat(found).isPresent();
        Account loaded = found.get();
        assertThat(loaded.getActiveStatus()).isEqualTo("Y");
        assertThat(loaded.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("1234.56"));
        assertThat(loaded.getCreditLimit()).isEqualByComparingTo(new BigDecimal("5000.00"));
        assertThat(loaded.getCashCreditLimit()).isEqualByComparingTo(new BigDecimal("1500.00"));
        assertThat(loaded.getOpenDate()).isEqualTo(LocalDate.of(2020, 1, 15));
        assertThat(loaded.getExpirationDate()).isEqualTo(LocalDate.of(2025, 12, 31));
        assertThat(loaded.getCurrentCycleCredit()).isEqualByComparingTo(new BigDecimal("100.00"));
        assertThat(loaded.getCurrentCycleDebit()).isEqualByComparingTo(new BigDecimal("200.00"));
        assertThat(loaded.getAddressZip()).isEqualTo("07001");
        assertThat(loaded.getGroupId()).isEqualTo("GRP001");
    }

    @Test
    void findById_returnsEmptyForMissing() {
        JpaAccountRepository repo = new JpaAccountRepository(em);
        Optional<Account> found = repo.findById(99999999L);
        assertThat(found).isEmpty();
    }

    @Test
    void save_updatesExistingAccount() {
        Account a = buildAccount(90000002L);
        em.persist(a);
        em.flush();
        em.clear();

        JpaAccountRepository repo = new JpaAccountRepository(em);
        Account loaded = repo.findById(90000002L).orElseThrow();
        loaded.setCurrentBalance(new BigDecimal("999.99"));
        repo.save(loaded);
        em.flush();
        em.clear();

        Account reloaded = em.find(Account.class, 90000002L);
        assertThat(reloaded.getCurrentBalance()).isEqualByComparingTo(new BigDecimal("999.99"));
    }
}
