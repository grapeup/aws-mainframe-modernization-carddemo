package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.UnitOfWork;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;

public class JpaUnitOfWork implements UnitOfWork {

    @PersistenceContext
    private EntityManager em;

    public JpaUnitOfWork() {}

    public JpaUnitOfWork(EntityManager em) {
        this.em = em;
    }

    @Override
    public void begin() {
        // When running inside a Spring-managed EntityManager, the real transaction
        // is controlled by the platform transaction manager. We use
        // EntityManager.joinTransaction() if one is already active, or rely on
        // the @Transactional boundary set up by the configuration.
        // For the explicit-control contract, we use the EntityTransaction API
        // when the EntityManager is application-managed (tests with manual EM),
        // or we set the connection isolation level via a native query.
        if (!em.isJoinedToTransaction()) {
            try {
                em.getTransaction().begin();
            } catch (IllegalStateException e) {
                // JTA-managed or already in a transaction — join it
                em.joinTransaction();
            }
        }
        // Set READ COMMITTED isolation — row locks (FOR UPDATE) handle serialisation.
        em.createNativeQuery("SET TRANSACTION ISOLATION LEVEL READ COMMITTED")
                .executeUpdate();
    }

    @Override
    public void commit() {
        em.flush();
        try {
            if (em.getTransaction().isActive()) {
                em.getTransaction().commit();
            }
        } catch (IllegalStateException e) {
            // JTA-managed — commit handled externally
        }
    }

    @Override
    public void rollback() {
        try {
            if (em.getTransaction().isActive()) {
                em.getTransaction().rollback();
            }
        } catch (IllegalStateException e) {
            // JTA-managed — rollback handled externally
        }
    }
}
