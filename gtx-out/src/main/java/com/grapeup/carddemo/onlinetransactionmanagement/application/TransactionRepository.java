package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Transaction;
import java.util.Optional;

/**
 * Repository for transaction persistence and ID generation.
 * Implementations stage changes; the {@link UnitOfWork} controls commit.
 */
public interface TransactionRepository {

    /**
     * Returns the highest transaction ID currently stored, or empty if none exist.
     * <p>
     * This read MUST occur inside a {@link UnitOfWork} transaction so that the
     * row lock (in a real database) is held until commit, preventing two callers
     * from reading the same maximum.
     * </p>
     */
    Optional<String> findMaxTransactionId();

    /**
     * Stages the transaction for persistence.
     * The change is only durable after {@link UnitOfWork#commit()}.
     */
    void save(Transaction transaction);
}
