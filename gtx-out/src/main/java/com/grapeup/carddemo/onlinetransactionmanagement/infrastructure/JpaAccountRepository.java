package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.AccountRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.Account;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;

import java.util.Optional;

public class JpaAccountRepository implements AccountRepository {

    @PersistenceContext
    private EntityManager em;

    public JpaAccountRepository() {}

    public JpaAccountRepository(EntityManager em) {
        this.em = em;
    }

    @Override
    public Optional<Account> findById(Long id) {
        return Optional.ofNullable(em.find(Account.class, id));
    }

    @Override
    public void save(Account account) {
        if (em.find(Account.class, account.getId()) == null) {
            em.persist(account);
        } else {
            em.merge(account);
        }
    }
}
