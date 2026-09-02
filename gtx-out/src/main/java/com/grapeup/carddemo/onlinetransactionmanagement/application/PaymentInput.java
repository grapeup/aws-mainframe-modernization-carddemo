package com.grapeup.carddemo.onlinetransactionmanagement.application;

/**
 * Input for the make-payment use case.
 * Account ID may be null or blank — the service validates it.
 * Confirmation must be "Y" or "y" to proceed.
 */
public record PaymentInput(
        String accountId,
        String confirmation
) {}
