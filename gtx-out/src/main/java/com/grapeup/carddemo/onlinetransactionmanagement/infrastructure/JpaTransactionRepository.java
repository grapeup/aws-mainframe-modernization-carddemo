package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;

import java.util.List;
import java.util.Optional;

public class JpaTransactionRepository implements TransactionRepository {

    @PersistenceContext
    private EntityManager em;

    public JpaTransactionRepository() {}

    public JpaTransactionRepository(EntityManager em) {
        this.em = em;
    }

    /**
     * Advisory lock key for transaction-id generation. Any constant will do; it
     * only has to be the same for every caller that generates an id.
     */
    private static final long TRANSACTION_ID_LOCK = 81_472_001L;

    @Override
    public Optional<String> findMaxTransactionId() {
        // Serialise id generation on a transaction-scoped ADVISORY lock, not on the
        // row that currently holds the maximum.
        //
        // Locking that row looks equivalent and is not. `ORDER BY id DESC LIMIT 1
        // FOR UPDATE` chooses the row using this transaction's original snapshot;
        // when it blocks behind another writer, PostgreSQL rechecks the locked row
        // but does NOT re-run the ORDER BY to find the new maximum, so once the
        // other transaction commits this returns the STALE maximum. The caller then
        // computes the same next id, and save() merges over the row that already
        // exists instead of inserting - five concurrent callers produced two rows.
        //
        // pg_advisory_xact_lock is taken BEFORE the read, so the read runs in a
        // fresh statement snapshot and sees the committed maximum. The lock is held
        // to the end of the transaction, which is the boundary UnitOfWork owns.
        em.createNativeQuery("SELECT pg_advisory_xact_lock(?1)")
                .setParameter(1, TRANSACTION_ID_LOCK)
                .getResultList();
        @SuppressWarnings("unchecked")
        List<String> results = em.createNativeQuery(
                        "SELECT id FROM transactions ORDER BY id DESC LIMIT 1")
                .getResultList();
        if (results.isEmpty()) {
            return Optional.empty();
        }
        return Optional.of(results.get(0).trim());
    }

    @Override
    public void save(Transaction transaction) {
        // A transaction record is append-only: this feature creates them and never
        // revises one. The previous find-then-merge turned a duplicate id into a
        // silent OVERWRITE of an existing record - no exception, no failed test,
        // one row where two belonged. Insert, and let the primary key object if the
        // id is not new, because at that point something upstream is wrong and the
        // useful outcome is a loud failure rather than lost data.
        em.persist(transaction);
    }
}
