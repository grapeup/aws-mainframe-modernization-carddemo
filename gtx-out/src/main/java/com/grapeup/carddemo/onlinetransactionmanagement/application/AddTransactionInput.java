package com.grapeup.carddemo.onlinetransactionmanagement.application;

import java.util.Objects;

/**
 * Input for the add-transaction use case.
 * <p>
 * The caller supplies either an 11-character account ID or a 16-character card number.
 * Amount is passed as a raw string so the service can validate its format.
 * All merchant fields are optional (some transactions have no physical merchant).
 * </p>
 */
public record AddTransactionInput(
        String accountIdOrCardNumber,
        String typeCode,
        short categoryCode,
        String source,
        String description,
        String amount,
        String originDate,
        String processDate,
        Integer merchantId,
        String merchantName,
        String merchantCity,
        String merchantZip,
        String cardNumber
) {
    public AddTransactionInput {
        Objects.requireNonNull(accountIdOrCardNumber, "accountIdOrCardNumber is required");
        Objects.requireNonNull(typeCode, "typeCode is required");
        Objects.requireNonNull(source, "source is required");
        Objects.requireNonNull(description, "description is required");
        Objects.requireNonNull(amount, "amount is required");
    }
}
