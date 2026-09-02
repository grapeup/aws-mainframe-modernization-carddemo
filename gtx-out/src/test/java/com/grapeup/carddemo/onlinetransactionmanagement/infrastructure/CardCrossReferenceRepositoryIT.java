package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
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
class CardCrossReferenceRepositoryIT {

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
    void findByCardNumber_returnsPersistedRef() {
        Account a = buildAccount(80000001L);
        em.persist(a);

        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber("4000000000000001");
        ref.setCustomerId(1001);
        ref.setAccountId(80000001L);
        em.persist(ref);
        em.flush();
        em.clear();

        JpaCardCrossReferenceRepository repo = new JpaCardCrossReferenceRepository(em);
        Optional<CardCrossReference> found = repo.findByCardNumber("4000000000000001");

        assertThat(found).isPresent();
        assertThat(found.get().getAccountId()).isEqualTo(80000001L);
        assertThat(found.get().getCustomerId()).isEqualTo(1001);
    }

    @Test
    void findByCardNumber_returnsEmptyForMissing() {
        JpaCardCrossReferenceRepository repo = new JpaCardCrossReferenceRepository(em);
        Optional<CardCrossReference> found = repo.findByCardNumber("9999999999999999");
        assertThat(found).isEmpty();
    }

    @Test
    void findByAccountId_returnsRef() {
        Account a = buildAccount(80000002L);
        em.persist(a);

        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber("4000000000000002");
        ref.setCustomerId(1002);
        ref.setAccountId(80000002L);
        em.persist(ref);
        em.flush();
        em.clear();

        JpaCardCrossReferenceRepository repo = new JpaCardCrossReferenceRepository(em);
        Optional<CardCrossReference> found = repo.findByAccountId(80000002L);

        assertThat(found).isPresent();
        assertThat(found.get().getCardNumber()).isEqualTo("4000000000000002");
    }
}
