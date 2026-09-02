package com.grapeup.carddemo.onlinetransactionmanagement.application;

/**
 * Use case interface for online transaction management.
 * Covers adding a new transaction and making an online bill payment.
 */
public interface TransactionUseCase {

    /**
     * Adds a new transaction against an account/card.
     * Validates the account is active, the amount format is correct,
     * generates a unique transaction ID, creates the transaction record,
     * and updates the account balance and cycle totals atomically.
     */
    AddTransactionResult addTransaction(AddTransactionInput input);

    /**
     * Makes an online bill payment that reduces the account balance to zero.
     * Validates the account exists, has a positive balance, and the payment
     * is confirmed before proceeding.
     */
    PaymentResult makePayment(PaymentInput input);
}
