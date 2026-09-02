package com.grapeup.carddemo.onlinetransactionmanagement.infrastructure;

import com.grapeup.carddemo.onlinetransactionmanagement.application.CardCrossReferenceRepository;
import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import jakarta.persistence.EntityManager;
import jakarta.persistence.PersistenceContext;
import jakarta.persistence.TypedQuery;

import java.util.List;
import java.util.Optional;

public class JpaCardCrossReferenceRepository implements CardCrossReferenceRepository {

    @PersistenceContext
    private EntityManager em;

    public JpaCardCrossReferenceRepository() {}

    public JpaCardCrossReferenceRepository(EntityManager em) {
        this.em = em;
    }

    @Override
    public Optional<CardCrossReference> findByCardNumber(String cardNumber) {
        return Optional.ofNullable(em.find(CardCrossReference.class, cardNumber));
    }

    @Override
    public Optional<CardCrossReference> findByAccountId(Long accountId) {
        TypedQuery<CardCrossReference> q = em.createQuery(
                "SELECT c FROM CardCrossReference c WHERE c.accountId = :accountId",
                CardCrossReference.class);
        q.setParameter("accountId", accountId);
        q.setMaxResults(1);
        List<CardCrossReference> results = q.getResultList();
        return results.isEmpty() ? Optional.empty() : Optional.of(results.get(0));
    }
}
