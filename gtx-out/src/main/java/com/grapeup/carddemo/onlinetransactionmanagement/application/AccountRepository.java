package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import java.util.Optional;

/**
 * Repository for account lookup and persistence.
 * Implementations stage changes; the {@link UnitOfWork} controls commit.
 */
public interface AccountRepository {

    Optional<Account> findById(Long id);

    /**
     * Stages the updated account for persistence.
     * The change is only durable after {@link UnitOfWork#commit()}.
     */
    void save(Account account);
}
