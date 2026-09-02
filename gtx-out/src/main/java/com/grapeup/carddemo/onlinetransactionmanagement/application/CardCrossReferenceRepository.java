package com.grapeup.carddemo.onlinetransactionmanagement.application;

import com.grapeup.carddemo.onlinetransactionmanagement.domain.CardCrossReference;
import java.util.Optional;

/**
 * Repository for card-to-account cross-reference lookup.
 */
public interface CardCrossReferenceRepository {

    Optional<CardCrossReference> findByCardNumber(String cardNumber);

    /**
     * Finds any card associated with the given account.
     * Used when the caller provides an account ID and we need the card number
     * for the transaction record.
     */
    Optional<CardCrossReference> findByAccountId(Long accountId);
}
