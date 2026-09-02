package com.grapeup.carddemo.onlinetransactionmanagement.application;

import java.math.BigDecimal;

/**
 * Result of the make-payment use case.
 * On success, contains the generated transaction ID and the amount paid.
 * On failure, contains the error code.
 */
public record PaymentResult(
        boolean success,
        String transactionId,
        BigDecimal amountPaid,
        TransactionError error
) {
    public static PaymentResult ok(String transactionId, BigDecimal amountPaid) {
        return new PaymentResult(true, transactionId, amountPaid, null);
    }

    public static PaymentResult fail(TransactionError error) {
        return new PaymentResult(false, null, null, error);
    }
}
