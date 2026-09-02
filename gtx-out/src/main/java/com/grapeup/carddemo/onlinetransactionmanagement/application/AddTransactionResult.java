package com.grapeup.carddemo.onlinetransactionmanagement.application;

import java.math.BigDecimal;
import java.util.Optional;

/**
 * Result of the add-transaction use case.
 * On success, contains the generated transaction ID and updated balance.
 * On failure, contains the error code.
 */
public record AddTransactionResult(
        boolean success,
        String transactionId,
        BigDecimal updatedBalance,
        TransactionError error
) {
    public static AddTransactionResult ok(String transactionId, BigDecimal updatedBalance) {
        return new AddTransactionResult(true, transactionId, updatedBalance, null);
    }

    public static AddTransactionResult fail(TransactionError error) {
        return new AddTransactionResult(false, null, null, error);
    }
}
