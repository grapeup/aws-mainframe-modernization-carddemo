package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.TransactionRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import jakarta.persistence.EntityManager;
import jakarta.persistence.LockModeType;
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

    @Override
    public Optional<String> findMaxTransactionId() {
        // Cannot use FOR UPDATE on an aggregate (SELECT MAX ... FOR UPDATE → 0A000).
        // Instead, lock the row with the highest id.
        @SuppressWarnings("unchecked")
        List<String> results = em.createNativeQuery(
                        "SELECT id FROM transactions ORDER BY id DESC LIMIT 1 FOR UPDATE")
                .getResultList();
        if (results.isEmpty()) {
            return Optional.empty();
        }
        return Optional.of(results.get(0).trim());
    }

    @Override
    public void save(Transaction transaction) {
        if (em.find(Transaction.class, transaction.getId()) == null) {
            em.persist(transaction);
        } else {
            em.merge(transaction);
        }
    }
}
