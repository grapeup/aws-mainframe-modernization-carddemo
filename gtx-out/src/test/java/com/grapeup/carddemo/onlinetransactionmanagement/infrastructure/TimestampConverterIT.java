package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import org.junit.jupiter.api.Test;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.test.context.DynamicPropertyRegistry;
import org.springframework.test.context.DynamicPropertySource;
import org.springframework.transaction.annotation.Transactional;

import java.math.BigDecimal;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.OffsetDateTime;
import java.time.ZoneId;
import java.time.ZoneOffset;

import static org.assertj.core.api.Assertions.assertThat;

@SpringBootTest
@Transactional
class TimestampConverterIT {

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

    private void insertParentChain(long accountId, String cardNumber) {
        Account a = new Account();
        a.setId(accountId);
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
        em.persist(a);

        CardCrossReference ref = new CardCrossReference();
        ref.setCardNumber(cardNumber);
        ref.setCustomerId(7001);
        ref.setAccountId(accountId);
        em.persist(ref);
        em.flush();
    }

    @Test
    void winterTimestamp_convertsWithCETOffset() {
        // 2024-01-15T14:30:00 in Europe/Berlin is CET = UTC+1
        // So UTC should be 13:30:00
        LocalDateTime winter = LocalDateTime.of(2024, 1, 15, 14, 30, 0);
        OffsetDateTime converted = TimestampConverter.toOffset(winter);

        assertThat(converted.getOffset()).isEqualTo(ZoneOffset.ofHours(1));
        assertThat(converted.toInstant())
                .isEqualTo(OffsetDateTime.of(2024, 1, 15, 13, 30, 0, 0, ZoneOffset.UTC).toInstant());

        // Round-trip through the database
        insertParentChain(75000001L, "7500000000000001");

        Transaction txn = new Transaction();
        txn.setId("0000000000200001");
        txn.setTypeCode("01");
        txn.setCategoryCode((short) 1);
        txn.setSource("ONLINE");
        txn.setDescription("WINTER TZ TEST");
        txn.setAmount(new BigDecimal("10.00"));
        txn.setCardNumber("7500000000000001");
        txn.setOriginatedAt(converted);
        em.persist(txn);
        em.flush();
        em.clear();

        Transaction loaded = em.find(Transaction.class, "0000000000200001");
        // The instant must be preserved regardless of what offset PG returns
        assertThat(loaded.getOriginatedAt().toInstant())
                .isEqualTo(converted.toInstant());
    }

    @Test
    void summerTimestamp_convertsWithCESTOffset() {
        // 2024-07-15T14:30:00 in Europe/Berlin is CEST = UTC+2
        // So UTC should be 12:30:00
        LocalDateTime summer = LocalDateTime.of(2024, 7, 15, 14, 30, 0);
        OffsetDateTime converted = TimestampConverter.toOffset(summer);

        assertThat(converted.getOffset()).isEqualTo(ZoneOffset.ofHours(2));
        assertThat(converted.toInstant())
                .isEqualTo(OffsetDateTime.of(2024, 7, 15, 12, 30, 0, 0, ZoneOffset.UTC).toInstant());

        // Round-trip through the database
        insertParentChain(75000002L, "7500000000000002");

        Transaction txn = new Transaction();
        txn.setId("0000000000200002");
        txn.setTypeCode("01");
        txn.setCategoryCode((short) 1);
        txn.setSource("ONLINE");
        txn.setDescription("SUMMER TZ TEST");
        txn.setAmount(new BigDecimal("10.00"));
        txn.setCardNumber("7500000000000002");
        txn.setOriginatedAt(converted);
        em.persist(txn);
        em.flush();
        em.clear();

        Transaction loaded = em.find(Transaction.class, "0000000000200002");
        assertThat(loaded.getOriginatedAt().toInstant())
                .isEqualTo(converted.toInstant());
    }

    @Test
    void winterAndSummerOffsetsDiffer() {
        LocalDateTime winter = LocalDateTime.of(2024, 1, 15, 12, 0, 0);
        LocalDateTime summer = LocalDateTime.of(2024, 7, 15, 12, 0, 0);

        OffsetDateTime winterOdt = TimestampConverter.toOffset(winter);
        OffsetDateTime summerOdt = TimestampConverter.toOffset(summer);

        // CET is +01:00, CEST is +02:00 — they must differ
        assertThat(winterOdt.getOffset()).isNotEqualTo(summerOdt.getOffset());
    }
}
