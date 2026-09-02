package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;

import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Optional;

/**
 * In-memory fake for {@link CardCrossReferenceRepository}.
 */
class InMemoryCardCrossReferenceRepository implements CardCrossReferenceRepository {

    private final Map<String, CardCrossReference> byCard = new LinkedHashMap<>();
    private final Map<Long, CardCrossReference> byAccount = new LinkedHashMap<>();

    void seed(CardCrossReference ref) {
        byCard.put(ref.getCardNumber(), ref);
        byAccount.put(ref.getAccountId(), ref);
    }

    @Override
    public Optional<CardCrossReference> findByCardNumber(String cardNumber) {
        return Optional.ofNullable(byCard.get(cardNumber));
    }

    @Override
    public Optional<CardCrossReference> findByAccountId(Long accountId) {
        return Optional.ofNullable(byAccount.get(accountId));
    }
}
